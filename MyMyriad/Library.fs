﻿namespace MyMyriad

open System
open FSharp.Compiler.SyntaxTree
open FSharp.Compiler.XmlDoc
open FsAst
open Myriad.Core
open FSharp.Compiler.Range

[<RequireQualifiedAccess>]
module Generator =
    /// Instructs to generate lenses for each property of the record
    type LensesAttribute(wrapperName : string) =
        inherit Attribute()
        let mutable _wrapperName = wrapperName
        member this.WrapperName = _wrapperName
        new () = LensesAttribute(null : string)
        new (``type``: Type) = LensesAttribute(``type``.Name)

module internal CreateLenses =
    let r = range0

    let private wrap lens (wrapperName : Option<string>) =
        match wrapperName with
        | Some name when not (String.IsNullOrWhiteSpace(name)) ->
            let wrapperVar = SynExpr.CreateLongIdent (false, LongIdentWithDots (Ident.CreateLong name, []), None)
            SynExpr.App (ExprAtomicFlag.NonAtomic, false, wrapperVar, SynExpr.CreateParen lens, r)
        | _ -> lens

    let private createLensForRecordField (parent: LongIdent) (wrapperName : Option<string>) (field: SynField) =
        let field = field.ToRcd
        let fieldName = match field.Id with None -> failwith "no field name" | Some f -> f

        let recordType =
            LongIdentWithDots.Create (parent |> List.map (fun i -> i.idText))
            |> SynType.CreateLongIdent

        let pattern =
            let name = LongIdentWithDots.CreateString fieldName.idText
            SynPatRcd.CreateLongIdent(name, [])

        let expr =
            let srcVarName = "x"
            let srcIdent = Ident.Create srcVarName

            // x.Property
            let getBody = LongIdentWithDots.Create [srcVarName; fieldName.idText]
            let recordArg = SynSimplePat.Typed(SynSimplePat.Id (srcIdent, None, false, false, false, r), recordType, r)
            // (x : Record)
            let getArgs = SynSimplePats.SimplePats ([recordArg], r)
            // fun (x : Record) -> x.Property
            let get = SynExpr.Lambda (false, false, getArgs, SynExpr.CreateLongIdent(false, getBody, None), r)

            let valueIdent = Ident.Create "value"
            let valuePattern = SynSimplePat.Typed(SynSimplePat.Id (valueIdent, None, false, false, false, r), field.Type, r)
            // (value : PropertyType)
            let valueArgPatterns = SynSimplePats.SimplePats ([valuePattern], r)
            let copySrc = SynExpr.CreateLongIdent(false, LongIdentWithDots.Create [srcVarName], None)
            let recordToUpdateName : RecordFieldName = (LongIdentWithDots.CreateString fieldName.idText, true)
            // { x with Property = value }
            let recordUpdate =
                SynExpr.CreateRecordUpdate (copySrc, [(recordToUpdateName, SynExpr.Ident valueIdent |> Some, None)])

            // (value : PropertyType) -> { x with Property = value }
            let innerLambdaWithValue =
                SynExpr.Lambda (false, true, valueArgPatterns, recordUpdate, r)

            // fun (x : Record) (value : PropertyType) -> { x with Property = value }
            let set =
                SynExpr.Lambda (false, true, getArgs, innerLambdaWithValue, r)

            let tuple =
                SynExpr.CreateTuple [
                    get
                    set
                ]

            wrap tuple wrapperName

        SynModuleDecl.CreateLet [{SynBindingRcd.Let with
                                    Pattern = pattern
                                    Expr = expr }]

    let private createLensForDU (requiresQualifiedAccess : bool) (parent: LongIdent) (wrapperName : Option<string>) (du : SynUnionCase) =
        let duRCD = du.ToRcd
        let singleCase =
            match duRCD.Type with
            | UnionCaseFields ([singleCase]) -> singleCase
            | UnionCaseFields (_ :: _) -> failwith "It is impossible to create a lens for a DU with several cases"
            | _ -> failwithf "Unsupported type"

        let duType =
            LongIdentWithDots.Create (parent |> List.map (fun i -> i.idText))
            |> SynType.CreateLongIdent

        let getterName = Ident("getter", range.Zero)
        let pattern =
            SynPatRcd.CreateLongIdent(LongIdentWithDots.CreateString "Lens'", [])

        let matchCaseIdentParts =
            if requiresQualifiedAccess then
                (parent |> List.map (fun i -> i.idText)) @ [duRCD.Id.idText]
            else
                [duRCD.Id.idText]

        // The name of the DU case, optionally preceded by the name of the DU itself, if
        // fully qualified access is required
        let fullCaseName = LongIdentWithDots.Create matchCaseIdentParts

        let lensExpression =
            let matchCase =
                let caseVariableName = "x"
                let args = [SynPatRcd.CreateLongIdent (LongIdentWithDots.CreateString caseVariableName, [])]
                let matchCaseIdent = SynPatRcd.CreateLongIdent(fullCaseName, args)

                let rhs = SynExpr.CreateIdent (Ident.Create caseVariableName)
                SynMatchClause.Clause(matchCaseIdent.FromRcd, None, rhs, range.Zero, DebugPointForTarget.No)

            let getterArgName = "x"
            let matchOn =
                let ident = LongIdentWithDots.CreateString getterArgName
                SynExpr.CreateLongIdent(false, ident, None)

            let matchExpression = SynExpr.Match(NoDebugPointAtLetBinding, matchOn, [matchCase], range.Zero)

            let setter =
                let valueIdent = Ident.Create "value"
                let valuePattern = SynSimplePat.Typed(SynSimplePat.Id (valueIdent, None, false, false, false, r), singleCase.ToRcd.Type, r)
                let valueArgPatterns = SynSimplePats.SimplePats ([valuePattern], r)

                let duType =
                    LongIdentWithDots.Create (parent |> List.map (fun i -> i.idText))
                    |> SynType.CreateLongIdent

                let createCase =
                    SynExpr.App (ExprAtomicFlag.NonAtomic, false, SynExpr.LongIdent (false, fullCaseName, None, r), SynExpr.Ident valueIdent, r)
                let innerLambdaWithValue =
                    SynExpr.Lambda (false, true, valueArgPatterns, createCase, r)
                let recordArg = SynSimplePat.Typed(SynSimplePat.Id (Ident.Create "_", None, false, false, false, r), duType, r)
                let getArgs = SynSimplePats.SimplePats ([recordArg], r)

                SynExpr.Lambda (false, true, getArgs, innerLambdaWithValue, r)

            let tuple =
                SynExpr.CreateTuple [
                    SynExpr.Ident getterName
                    setter
                ]

            let getterLet =
                let valData = SynValData.SynValData(None, SynValInfo.Empty, None)
                let synPat = SynPat.Named(SynPat.Wild r, Ident.Create "x", false, None, r)
                let synPat = SynPat.Typed(synPat, duType, r)
                let synPat = SynPat.Paren (synPat, r)

                let synPat = SynPat.LongIdent (LongIdentWithDots.CreateString "getter", None, None, SynArgPats.Pats [synPat], None, r)

                SynBinding.Binding (None, SynBindingKind.NormalBinding, false, false, [], PreXmlDoc.Empty, valData, synPat, None, matchExpression, r, DebugPointForBinding.NoDebugPointAtDoBinding)

            let lens = SynExpr.LetOrUse (false, false, [getterLet], tuple, r)

            wrap lens wrapperName

        SynModuleDecl.CreateLet [{ SynBindingRcd.Let with
                                      Pattern = pattern
                                      Expr = lensExpression }]
    let private updateLastItem list updater =
        let folder item state =
            match state with
            | [] -> [updater item]
            | l -> item :: l

        List.foldBack folder list []

    let private (|LongIdentLid|) (ident : LongIdentWithDots) =
        ident.Lid

    let private (|SynTypeAppTypeName|_|) (expr : SynType) =
        match expr with
        | SynType.App (name, _, _, _, _, _, _) -> Some name
        | _ -> None

    let createLensModule (namespaceId: LongIdent) (typeDefn: SynTypeDefn, attr: SynAttribute) =
        let (TypeDefn(synComponentInfo, synTypeDefnRepr, _members, _range)) = typeDefn
        let (ComponentInfo(_attributes, _typeParams, _constraints, recordId, _doc, _preferPostfix, _access, _range)) = synComponentInfo

        // Append "Lenses" to the module name
        let moduleIdent = updateLastItem recordId (fun i -> Ident.Create (sprintf "%sLenses" i.idText))

        let wrapperName =
            match attr.ArgExpr with
            | SynExpr.Const (SynConst.Unit, _) -> None
            | SynExpr.Paren(SynExpr.Const ((SynConst.String(s, _)), _), _leftParenRange, _rightParenRange, _range) -> Some s
            | SynExpr.Paren(SynExpr.TypeApp (SynExpr.Ident ident, _, [SynTypeAppTypeName(SynType.LongIdent (LongIdentLid (wrapperIdent :: _)))], _, _, _, _), _, _, _)
                when ident.idText = "typedefof" || ident.idText = "typeof" ->
                Some wrapperIdent.idText
            | _ -> failwithf "Unsupported syntax of specifying the wrapper name for type %A." recordId

        let openParent = SynModuleDecl.CreateOpen (LongIdentWithDots.Create (namespaceId |> List.map (fun ident -> ident.idText)))
        let moduleInfo = SynComponentInfoRcd.Create moduleIdent

        match synTypeDefnRepr with
        | SynTypeDefnRepr.Simple(SynTypeDefnSimpleRepr.Record(_accessibility, recordFields, _recordRange), _range) ->
            let fieldLenses = recordFields |> List.map (createLensForRecordField recordId wrapperName)

            let declarations = [
                yield openParent
                yield! fieldLenses
            ]

            SynModuleDecl.CreateNestedModule(moduleInfo, declarations)
        | SynTypeDefnRepr.Simple(SynTypeDefnSimpleRepr.Union(_accessibility, [singleCase], _recordRange), _range) ->
            let requiresQualifiedAccess = Ast.getAttribute<RequireQualifiedAccessAttribute> typeDefn |> Option.isSome
            let lens = createLensForDU requiresQualifiedAccess recordId wrapperName singleCase

            let declarations = [
                openParent
                lens
            ]

            SynModuleDecl.CreateNestedModule(moduleInfo, declarations)
        | _ -> failwithf "%A is not a record type." recordId

[<MyriadGenerator("lenses")>]
type LensesGenerator() =

    interface IMyriadGenerator with
        member __.Generate(namespace', ast: ParsedInput) =
            let namespaceAndRecords = Ast.extractRecords ast
            let recordsModules =
                namespaceAndRecords
                |> List.collect (
                    fun (ns, records) ->
                    records
                    |> List.choose (fun r ->
                        let attr = Ast.getAttribute<Generator.LensesAttribute> r
                        Option.map (fun a -> r, a) attr)
                    |> List.map (CreateLenses.createLensModule ns))

            let namespaceAndDUs = Ast.extractDU ast
            let duModules =
                namespaceAndDUs
                |> List.collect (
                    fun (ns, dus) ->
                    dus
                    |> List.choose (fun du ->
                        let attr = Ast.getAttribute<Generator.LensesAttribute> du
                        Option.map (fun a -> du, a) attr)
                    |> List.map (CreateLenses.createLensModule ns))

            let namespaceOrModule =
                { SynModuleOrNamespaceRcd.CreateNamespace(Ident.CreateLong namespace')
                    with
                        IsRecursive = true
                        Declarations = recordsModules @ duModules }

            namespaceOrModule