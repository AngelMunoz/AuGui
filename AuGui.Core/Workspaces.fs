namespace AuGui.Core

open System.Text.Json
open System.Threading.Tasks
open FSharp.Control.Tasks

module Workspace =
  open System.IO
  open Node

  let private getJsonOptions () =
    let opts = JsonSerializerOptions()
    opts.AllowTrailingCommas <- true
    opts.PropertyNamingPolicy <- JsonNamingPolicy.CamelCase
    opts.ReadCommentHandling <- JsonCommentHandling.Skip
    opts.DictionaryKeyPolicy <- JsonNamingPolicy.CamelCase
    opts

  let GetDefaultWorkspacePath () =
    let personalPath =
      System.Environment.GetFolderPath
        (System.Environment.SpecialFolder.Personal)

    Path.Combine(personalPath, "AureliaProjects")

  let EnsureWorkspacePathExists (path: string) =
    try
      Ok(Directory.CreateDirectory(path))
    with ex -> Error ex

  let DetectAureliaProjects (rootPath: string) =
    let rootDir = DirectoryInfo(rootPath)

    let children = rootDir.GetDirectories()

    children
    |> Array.Parallel.map (fun dir ->
         let foundAureliaDir =
           dir.GetDirectories()
           |> Array.tryPick (fun directory ->
                directory.Name.Contains("aurelia_project")
                |> function
                | true -> Some directory
                | false -> None)

         let fileinfo =
           foundAureliaDir
           |> Option.map (fun dir ->
                dir.Parent.GetFiles()
                |> Array.tryPick (fun file ->
                     file.FullName.Contains("package.json")
                     |> function
                     | true -> Some file
                     | false -> None))
           |> Option.flatten

         match fileinfo with
         | Some fileinfo ->
             async {
               try
                 let content = fileinfo.OpenRead()
                 let opts = getJsonOptions ()
                 let! package =
                   JsonSerializer.DeserializeAsync<PackageJson>(content, opts)
                     .AsTask()
                   |> Async.AwaitTask

                 return Ok(fileinfo, package)
               with ex -> return Error ex.Message
             }
         | None ->
             Error
               """Project contains "aurelia_framework" directory but is missing package.json"""
             |> async.Return)
    |> Async.Parallel
