namespace AuGui.Core

open CliWrap
open CliWrap.Builders

[<RequireQualifiedAccess>]
module CliInterop =
  let private getExt (): string =
    match Platform.DetectPlatform() with
    | Platform.Windows -> ".cmd"
    | Platform.Linux
    | Platform.OSX
    | Platform.Other -> ""

  let newProjectCmd (name: string)
                    (template: ProjectType * Option<Template>)
                    (targetPath: string)
                    : Result<Command, string> =

    let command = sprintf "npx%s" (getExt ())

    let cmd =
      match template with
      | ProjectType.Babel, _ ->
          Ok
            (Cli.Wrap(command)
                .WithArguments([| "makes"; "aurelia"; name; "-s" |]))
      | ProjectType.Typescript, _ ->
          Ok
            (Cli.Wrap(command)
                .WithArguments([|
                  "makes"
                  "aurelia"
                  name
                  "-s typescript"
                |]))
      | ProjectType.Custom, Some template ->
          let opts = Template.GetCmdArgs template

          let args =
            [|
              "makes"
              "aurelia"
              name
              "-s"
              (opts |> String.concat ",")
            |]

          Ok(Cli.Wrap(command).WithArguments(args))
      | ProjectType.Custom, None ->
          Error "Can't create custom template without options"

    match cmd with
    | Ok command ->
        printfn "%A" command.EnvironmentVariables

        let configure (envBuilder: EnvironmentVariablesBuilder): unit =
          envBuilder.Set
            ("PATH", System.Environment.GetEnvironmentVariable("PATH"))
          |> ignore

        printfn "%A" command.EnvironmentVariables

        let command =
          command.WithWorkingDirectory(targetPath)
                 .WithEnvironmentVariables(configure)

        printfn "%A" command.EnvironmentVariables
        Ok command
    | Error err -> Error err
