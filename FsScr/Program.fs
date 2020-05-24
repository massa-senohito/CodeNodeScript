namespace ScrEngine

  open FSharp.Compiler.SourceCodeServices

  module Eval =
    open System
    open System.Diagnostics

    //let assSym = {
    //  FullName = @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\Common7\IDE\CommonExtensions\Microsoft\FSharp\FSharp.Core.dll"
    //  Kind = 
    //}
    let test2() =
      let testScript = """

let x = 3 + 4
let  f x = x + 4
let hoge = "hoge"
let huga = " fg"
f 2
//[hoge ;  huga ]
//hoge
"""
      let ev,err = FSTypeInfo.eval testScript
      match ev with
      |Ok(ret)->
        match ret with
        |Some(r)->
          FSTypeInfo.extractVal r
        |None->[]
      |Error(en) -> printfn "%A" en;[]
     
    let toolTipTest ()=
      let input =
        """
open System

let foo() = 
   let msg = String.Concat("Hello"," ","world")
   if true then 
     printfn "%s" msg.
        """
      let inputLines = input.Split('\n')
      FSTypeInfo.toolTip input 4 7 (inputLines.[1]) ["foo"] FSharpTokenTag.Identifier None

    let declTest ()=
      let input =
        """
let f (str:string) = str."
  //      """
  //let foo() = 
  //  let msg = String.Concat("Hello"," ","world")
  //  if true then 
  //    printfn "%s" msg.
  //      """
      let inputLines = input.Split('\n')
      let forth = 
        //inputLines.[4]
        inputLines.[0]
      let row =
        //23
        24
      let col = 1
      //for r in [20 .. 25] do
      let dec = FSTypeInfo.decls input col forth row |> Async.RunSynchronously
      //Trace.WriteLine( string row + " is " )
      for i in dec.Items do
        Trace.WriteLine( i.Name)
    //let f (s:string) = s.
    [<EntryPoint>]
    let main argv =
      //declTest()

      //let too = Async.RunSynchronously <| toolTipTest()
      //match too with
      //| FSharpToolTipText str  ->
      //  printfn "%A" <|str.ToString()
      printfn "%A" <|test2()
      let testScript = """
    module M
    
    type C() = 
       member x.P = 19
    
    let x = 3 + 4
    """
      let com = FSTypeInfo.compileCheck testScript
      match com with
      |Ok(signa,err)->
        let m = signa.Entities.[0]
        let c = m.NestedEntities.[0]
        let p = c.MembersFunctionsAndValues.[2]
        let pa = p.ReturnParameter
        printfn "%A" p
      |_ -> ()
      let errors, exitCode, dynAssembly = FSTypeInfo.compile testScript
      let asm = dynAssembly.Value
      let typeList = [for i in asm.DefinedTypes -> i]
      let memList = 
        [for i in typeList ->
          [for mem in i.GetMethods() -> mem ] ] |> List.concat
      let p = memList.Item 5
      let cInst = Activator.CreateInstance (typeList.Item 1).UnderlyingSystemType
      let xType = p.Invoke(cInst,[||])
      // reflect
      (*
      *)
      0 // return an integer exit code
