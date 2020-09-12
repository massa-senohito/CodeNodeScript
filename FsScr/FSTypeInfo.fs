namespace ScrEngine

  open FSharp.Compiler.Text
  open FSharp.Compiler.Interactive.Shell
  open FSharp.Compiler
  open System.Diagnostics
  
  module FSTypeInfo =
    open FSharp.Compiler.SourceCodeServices
    let file = "Test.fsx"
    let compileCheck input =
      let checker = FSharpChecker.Create()
      let inputS = SourceText.ofString(input)
      let res = async{
        let! opt,error = checker.GetProjectOptionsFromScript(file , inputS )
        let! parseResult,answer = checker.ParseAndCheckFileInProject(file , 0 , inputS , opt )
        let res =
          match answer with
          |FSharpCheckFileAnswer.Succeeded(checkResult) -> Ok( checkResult.PartialAssemblySignature , error)
          |FSharpCheckFileAnswer.Aborted -> Error(error)
        return res
      }
      res |> Async.RunSynchronously

    let checkResult input = 
      let checker = FSharpChecker.Create()
      let inputS = SourceText.ofString(input)
      let file = "Test.fsx"
      let res = async{
        let! opt,error = checker.GetProjectOptionsFromScript(file , inputS )
        let parsingOptions, _errors2 = checker.GetParsingOptionsFromProjectOptions(opt)
        let! parseFileResults = checker.ParseFile(file , inputS , parsingOptions)
        let! checkFileAnswer = checker.CheckFileInProject(parseFileResults, file, 0, inputS, opt) 
        let ans = 
          match checkFileAnswer with
          | FSharpCheckFileAnswer.Succeeded(res) -> Some <| res 
          | FSharpCheckFileAnswer.Aborted -> None
        return ans,parseFileResults
      }
      res //|> Async.RunSynchronously

    // 型を示すツールチップを表示
    let toolTip input line col lineTxt tokenList tokenTag (userOp : string option) =
      let getTool (v:FSharpCheckFileResults) =
        if userOp.IsSome then
          v.GetToolTipText(line , col , lineTxt , tokenList , tokenTag , userOp.Value)
        else 
          v.GetToolTipText(line , col , lineTxt , tokenList , tokenTag )
      async{
        let! res,fileRes = checkResult input
        let! toolStr = 
          match res with
          | Some v -> getTool v
          | None   -> failwith "check Fail"
        return toolStr
      }

    // 自動補完の情報
    let decls input line lineText row=
      //let s = new FSharp.Compiler.SourceCodeServices.AssemblySymbol("dll")
      let hasCheck x = false
      async{
        let! res,fileRes = checkResult input
        let! declList = 
          match res with
          | Some v -> v.GetDeclarationListInfo( (Some fileRes) , line , lineText , PartialLongName.Empty row,(fun () -> []) , hasCheck) 
          | None   -> failwith "check Fail"
        return declList
      }
      

    open System.IO
    open System.Text
    // 入出力のストリームを初期化
    let sbOut = new StringBuilder()
    let sbErr = new StringBuilder()
    let inStream = new StringReader("")
    let outStream = new StringWriter(sbOut)
    let errStream = new StringWriter(sbErr)
    
    // コマンドライン引数を組み立てて、FSIセッションを開始する
    let argv = [| "C:\\fsi.exe" |]//; "--noninteractive"|]
    let allArgs = Array.append argv [||]
      //[|@"-r:C:\hogefuga\.nuget\packages\fsharp.compiler.service\35.0.0\lib\net461\FSharp.Compiler.Service.dll"|]//
    
    let fsiConfig = FsiEvaluationSession.GetDefaultConfiguration()
    let mutable fsi = None
    let reset ()= fsi <- Some <| FsiEvaluationSession.Create(fsiConfig, allArgs, inStream, outStream, errStream)

    // eval
    let eval text =
      let fsiIn =
        match fsi with
        |Some x-> x
        |None->reset();fsi.Value
      let res,errList = 
        // こちらは#r対応しない
        //fsiIn.EvalExpressionNonThrowing(text)
        fsiIn.EvalInteractionNonThrowing(text)
      let result,err =
        match res with
        | Choice1Of2 valueOpt -> Ok valueOpt,errList
        | Choice2Of2 (exn:exn) -> Error exn,errList
      // コンパイルエラーになってもOkになる
      if err.Length > 0 then
        Error null,err
      else
        result,err

    let compile text =
      let scs = FSharpChecker.Create()
      let fn = Path.GetTempFileName()
      let fn2 = Path.ChangeExtension(fn, ".fs")
      let fn3 = Path.ChangeExtension(fn, ".dll")
      File.WriteAllText(fn2, text)
      let errors, exitCode, dynAssembly = 
        scs.CompileToDynamicAssembly([| "-o"; fn3; "-a"; fn2 |], Some(stdout,stderr)) |> Async.RunSynchronously
      errors,exitCode,dynAssembly

    open FSharp.Compiler.Interactive
    open System.Text.Json

    let extractVal (r:Shell.FsiValue)=
      let t = r.ReflectionValue
      let isType = 
        match t with
        | :? System.Type -> true
        | _-> false
      match t with
      | :? System.String as v ->
        [v]
      | :? System.Collections.IEnumerable  as v ->
        [for i in v -> i.ToString()]
        // 一部のメソッド (GetType)で IsGenericParameterがtrueでないと怒られる
      | t when not isType->
        //[ JsonConvert.SerializeObject(t).ToString()]
        // こちらのほうが詳しいクラス情報もらえるので使用
        [JsonSerializer.Serialize(t)]
      | t ->
        [t.ToString()]
    type EvalResult() =
      let mutable err = [||]
      let mutable result = None
      let mutable except = None
      member t.Eval text =
        let ev,error = eval text
        err <- error
        match ev with
        | Ok v     ->
          result <- v
          err <- [||]
          except <- None
        | Error ex ->
          result <- None
          except <- Some ex
      member t.Error  =
        err
      member t.Except =
        match except with
        |Some ex->
          result <- None
          // コンパイルエラーになってもOKの時null
          if(ex <> null) then
            ex.ToString()
          else ""
        |None   -> ""
      member t.Result =
        match result with
        |Some v -> extractVal v 
        |None   -> []
  
