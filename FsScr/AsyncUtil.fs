module AsyncUtil
  open System.Threading.Tasks
  let defaultAsyncAsTask<'t> (t:'t Async)=
    FSharp.Control.Async.StartAsTask(t,TaskCreationOptions.None)
