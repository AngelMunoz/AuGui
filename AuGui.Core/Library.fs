namespace AuGui.Core

open System.Collections.Generic
open System.Text.Json
open System.Text.Json.Serialization
open System.Runtime.InteropServices


module Node =
  open System.IO
  open System.Threading.Tasks
  open FSharp.Control.Tasks

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

[<RequireQualifiedAccess>]
type ProjectType =
  | Babel
  | Typescript
  | Custom

[<RequireQualifiedAccess>]
type Bundler =
  | Dumber
  | Webpack

[<RequireQualifiedAccess>]
type Transpiler =
  | Babel
  | Typescript

[<RequireQualifiedAccess>]
type CssMode =
  | CssModule
  | ShadowDom

[<RequireQualifiedAccess>]
type CssPreprocessor =
  | Css
  | Sass
  | Less

[<RequireQualifiedAccess>]
type E2ETestingFx = | Cypress

[<RequireQualifiedAccess>]
type UnitTestingFx =
  | Jest
  | Jasmine
  | Mocha
  | Tape

[<RequireQualifiedAccess>]
type SampleCode =
  | AppMin
  | AppWithRouter

type Template =
  {
    Bundler: Bundler
    Transpiler: Transpiler
    CssPreprocessor: CssPreprocessor
    UnitTestingFx: UnitTestingFx
    E2ETesingFx: Option<E2ETestingFx>
    SampleCode: SampleCode
  }

  static member GetCmdArgs(template: Template): array<string> =
    let bundler =
      match template.Bundler with
      | Bundler.Webpack -> "webpack"
      | Bundler.Dumber -> "dumber"

    let transpiler =
      match template.Transpiler with
      | Transpiler.Typescript -> "typescript"
      | Transpiler.Babel -> "babel"

    let cssPreprocessor =
      match template.CssPreprocessor with
      | CssPreprocessor.Css -> "css"
      | CssPreprocessor.Sass -> "sass"
      | CssPreprocessor.Less -> "less"

    let testingFx =
      match template.UnitTestingFx with
      | UnitTestingFx.Jest -> "jest"
      | UnitTestingFx.Jasmine -> "jasmine"
      | UnitTestingFx.Mocha -> "mocha"
      | UnitTestingFx.Tape -> "tape"

    let e2eTestingFx =
      match template.E2ETesingFx with
      | Some fx ->
          match fx with
          | E2ETestingFx.Cypress -> "cypress"
      | None -> ""

    let sampleCode =
      match template.SampleCode with
      | SampleCode.AppMin -> "app-min"
      | SampleCode.AppWithRouter -> "app-with-router"

    [| sampleCode |]
    |> Array.append [|
         bundler
         transpiler
         cssPreprocessor
         testingFx
         if not (System.String.IsNullOrEmpty(e2eTestingFx))
         then e2eTestingFx
       |]
    |> Array.filter (System.String.IsNullOrEmpty >> not)



(*
  npx makes aurelia new-project-name -s dumber,css-module,sass,cypress,app-with-router
*)

type Project =
  {
    Name: string
    Path: string
    ProjectType: ProjectType
    Template: Option<Template>
    PackageJson: Option<Node.PackageJson>
  }

type Workspace =
  {
    Name: string
    Path: string
    Projects: seq<Project>
  }

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
