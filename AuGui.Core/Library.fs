namespace AuGui.Core

open System.Collections.Generic
open System.Text.Json
open System.Text.Json.Serialization
open System.Runtime.InteropServices


[<RequireQualifiedAccess>]
type Platform =
  | Windows
  | Linux
  | OSX
  | Other

  static member DetectPlatform() =
    if RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
    then Windows
    else if RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
    then Linux
    else if RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
    then OSX
    else Other


module Node =
  open System.IO
  open System.Threading.Tasks
  open FSharp.Control.Tasks

  [<RequireQualifiedAccess>]
  type PackageManager =
    | Npm
    | Pnpm
    member this.ToArgString() =
      match this with
      | Npm -> "npm"
      | Pnpm -> "pnpm"

  [<JsonFSharpConverter>]
  type PackageJson =
    {
      name: string
      description: string
      license: string
      dependencies: IDictionary<string, string>
      devDependencies: IDictionary<string, string>
      scripts: IDictionary<string, string>
    }
    static member ParseFile(path: string): Task<Result<PackageJson, string>> =
      task {
        let exists = File.Exists(path)
        if not exists then
          return Error "File Not Found"
        else
          try
            let content = File.OpenRead(path)
            let opts = JsonSerializerOptions()
            opts.AllowTrailingCommas <- true
            opts.PropertyNamingPolicy <- JsonNamingPolicy.CamelCase
            opts.ReadCommentHandling <- JsonCommentHandling.Skip
            opts.DictionaryKeyPolicy <- JsonNamingPolicy.CamelCase
            let! package = JsonSerializer.DeserializeAsync<PackageJson>(content)
            return Ok package
          with ex -> return Error ex.Message
      }


module AureliaCli =
  let private getExt (): string =
    match Platform.DetectPlatform() with
    | Platform.Windows -> ".cmd"
    | Platform.Linux
    | Platform.OSX
    | Platform.Other -> ""

  let baseCommand = sprintf "aurelia%s" (getExt ())

  module New =

    [<RequireQualifiedAccess>]
    type ProjectKind =
      | Babel
      | Typescript
      | Custom

    [<RequireQualifiedAccess>]
    type ProjectType =
      | App of ProjectKind
      | Plugin of ProjectKind

      member this.ToArgString() =
        match this with
        | App _ -> ""
        | Plugin _ -> "--plugin"

    [<RequireQualifiedAccess>]
    type CliBundlerType =
      | RequireJs
      | Alameda

      member this.ToArgString() =
        match this with
        | RequireJs -> ""
        | Alameda -> ",alameda"

    [<RequireQualifiedAccess>]
    type Bundler =
      | Webpack
      | CliBundler of CliBundlerType
      member this.ToArgString() =
        match this with
        | Webpack -> ""
        | CliBundler bundler -> sprintf "cli-bundler%s" (bundler.ToArgString())

    [<RequireQualifiedAccess>]
    type TargetPlatform =
      | Web
      | DotnetCore
      member this.ToArgString() =
        match this with
        | Web -> ""
        | DotnetCore -> "dotnet-core"

    [<RequireQualifiedAccess>]
    type Transpiler =
      | Babel
      | Typescript
      member this.ToArgString() =
        match this with
        | Babel -> ""
        | Typescript -> "typescript"

    [<RequireQualifiedAccess>]
    type HtmlMinifier =
      | None
      | HtmlMin
      member this.ToArgString() =
        match this with
        | None -> ""
        | HtmlMin -> "htmlmin"

    [<RequireQualifiedAccess>]
    type CssPreprocessor =
      | Css
      | Sass
      | Less
      | Stylus
      member this.ToArgString() =
        match this with
        | Css -> ""
        | Sass -> "sass"
        | Less -> "less"
        | Stylus -> "stylus"

    [<RequireQualifiedAccess>]
    type PostCSS =
      | None
      | PostCSS
      member this.ToArgString() =
        match this with
        | None -> ""
        | PostCSS -> "postcss"

    [<RequireQualifiedAccess>]
    type E2ETestingFx =
      | None
      | Cypress
      member this.ToArgString() =
        match this with
        | None -> ""
        | Cypress -> "cypress"

    [<RequireQualifiedAccess>]
    type UnitTestingFx =
      | None
      | Jest
      | Karma
      member this.ToArgString() =
        match this with
        | None -> ""
        | Jest -> "jest"
        | Karma -> "karma"

    [<RequireQualifiedAccess>]
    type CodeEditor =
      | None
      | VSCode
      member this.ToArgString() =
        match this with
        | None -> ""
        | VSCode -> "vscode"

    [<RequireQualifiedAccess>]
    type Scaffold =
      | ScaffoldMinimum
      | ScaffoldNavigation
      | PluginScaffoldMinimum
      | PluginScaffoldBasic
      member this.ToArgString() =
        match this with
        | ScaffoldMinimum -> "scaffold-minimum"
        | ScaffoldNavigation -> "scaffold-navigation"
        | PluginScaffoldMinimum -> "plugin-scaffold-minumum"
        | PluginScaffoldBasic -> "plugin-scaffold-basic"

    [<RequireQualifiedAccess>]
    type DockerFile =
      | No
      | Yes
      member this.ToArgString() =
        match this with
        | No -> ""
        | Yes -> "docker"

    type NewArgs =
      {
        Name: string
        ProjectType: ProjectType
        Bundler: Bundler
        TargetPlatform: TargetPlatform
        Transpiler: Transpiler
        HtmlMinifier: HtmlMinifier
        CssPreprocessor: CssPreprocessor
        PostCSS: PostCSS
        UnitTestingFx: UnitTestingFx
        E2ETestingFx: E2ETestingFx
        CodeEditor: CodeEditor
        Scaffold: Scaffold
        DockerFile: DockerFile
      }

  [<RequireQualifiedAccess>]
  type BuildFlags =
    | Analyze
    | Watch
    | Env of string
    member this.ToArgString() =
      match this with
      | Analyze -> "--analyze"
      | Watch -> "--watch"
      | Env env -> sprintf "--env %s" env

  [<RequireQualifiedAccess>]
  type RunFlags =
    | Analyze
    | HMR
    | Watch
    | Open
    | Port of int
    | Host of string
    | Env of string
    member this.ToArgString() =
      match this with
      | Analyze -> "--analyze"
      | Watch -> "--watch"
      | HMR -> "--hmr"
      | Open -> "--open"
      | Host host -> sprintf "--host %s" host
      | Port port -> sprintf "--port %i" port
      | Env env -> sprintf "--env %s" env

  [<RequireQualifiedAccess>]
  type Generator =
    | Attribute
    | BindingBehavior
    | Component of directory: Option<string>
    | Element
    | Generator
    | Task
    | ValueConverter
    | Custom of string
    member this.ToArgString() =
      match this with
      | Attribute -> "attribute"
      | BindingBehavior -> "binding-behavior"
      | Component _ -> "component"
      | Element -> "element"
      | Generator -> "generator"
      | Task -> "task"
      | ValueConverter -> "value-converter"
      | Custom custom -> custom

  [<RequireQualifiedAccess>]
  type AuCliCommand =
    | New of New.NewArgs
    (* These become available once npm install has been run in the project's directory *)
    | Build of Option<list<BuildFlags>>
    | Run of Option<list<RunFlags>>
    | Generate of Generator: Generator * Name: string

  module AuCliCommand =
    open New

    type PreCommand =
      {
        Command: string
        Args: array<string>
      }

    let private generateNewCommand (command: New.NewArgs): PreCommand =
      let args =
        match command.ProjectType with
        | ProjectType.App kind
        | ProjectType.Plugin kind ->
            match kind with
            | ProjectKind.Babel -> "jest,vscode"
            | ProjectKind.Typescript -> "typescript,jest,vscode"
            | ProjectKind.Custom ->
                [
                  command.ProjectType.ToArgString()
                  command.Bundler.ToArgString()
                  command.TargetPlatform.ToArgString()
                  command.Transpiler.ToArgString()
                  command.HtmlMinifier.ToArgString()
                  command.CssPreprocessor.ToArgString()
                  command.PostCSS.ToArgString()
                  command.UnitTestingFx.ToArgString()
                  command.E2ETestingFx.ToArgString()
                  command.CodeEditor.ToArgString()
                  command.Scaffold.ToArgString()
                  command.DockerFile.ToArgString()
                ]
                |> String.concat ","

      let args =
        match command.ProjectType with
        | ProjectType.Plugin _ -> sprintf "plugin,%s" args
        | _ -> args

      {
        Command = baseCommand
        Args = [| "new"; command.Name; "-s"; args |]
      }

    let GenerateCommand (command: AuCliCommand): PreCommand =
      match command with
      | AuCliCommand.New newArgs -> generateNewCommand newArgs
      | AuCliCommand.Build flags ->
          let flags =
            match flags with
            | Some flags ->
                flags
                |> List.map (fun s -> s.ToArgString())
                |> String.concat " "
            | None -> ""

          {
            Command = baseCommand
            Args = [| "build"; flags |]
          }
      | AuCliCommand.Run flags ->
          let flags =
            match flags with
            | Some flags ->
                flags
                |> List.map (fun s -> s.ToArgString())
                |> String.concat " "
            | None -> ""

          {
            Command = baseCommand
            Args = [| "build"; flags |]
          }
      | AuCliCommand.Generate (generator, name) ->
          let dir =
            match generator with
            | Generator.Component dir -> defaultArg dir ""
            | _ -> ""

          {
            Command = baseCommand
            Args =
              [|
                "generate"
                generator.ToArgString()
                dir
              |]
          }

type NewProject =
  {
    Name: string
    Path: string
    Template: Option<AureliaCli.New.NewArgs>
  }

type Project =
  {
    Path: string
    PackageJson: Node.PackageJson
  }

type Workspace =
  {
    Name: string
    Path: string
    Projects: seq<Project>
  }
