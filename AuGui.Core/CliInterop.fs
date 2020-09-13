namespace AuGui.Core

open CliWrap
open CliWrap.Builders
open CliWrap.Buffered
open FSharp.Control.Tasks
open AureliaCli

[<RequireQualifiedAccess>]
module CliInterop =
  let private addPathToCliWrapCmd (envBuilder: EnvironmentVariablesBuilder): unit =
    envBuilder.Set("PATH", System.Environment.GetEnvironmentVariable("PATH"))
    |> ignore

  let private baseCmd (packageManager: Node.PackageManager) =
    match Platform.DetectPlatform() with
    | Platform.Windows -> sprintf "%s.cmd" (packageManager.ToArgString())
    | Platform.OSX
    | Platform.Linux
    | Platform.Other -> sprintf "%s" (packageManager.ToArgString())


  let detectPackageManagers () =
    task {
      let! npm =
        Cli.Wrap(baseCmd Node.PackageManager.Npm)
           .WithEnvironmentVariables(addPathToCliWrapCmd)
           .WithValidation(CommandResultValidation.None).ExecuteBufferedAsync()

      let! pnpm =
        Cli.Wrap(baseCmd Node.PackageManager.Pnpm)
           .WithEnvironmentVariables(addPathToCliWrapCmd)
           .WithValidation(CommandResultValidation.None).ExecuteBufferedAsync()

      return [
        if npm.ExitCode = 0
           && not (System.String.IsNullOrWhiteSpace npm.StandardOutput) then
          Node.PackageManager.Npm
        if pnpm.ExitCode = 0
           && not (System.String.IsNullOrWhiteSpace pnpm.StandardOutput) then
          Node.PackageManager.Pnpm
      ]
    }

  let getInstallCliWrapCmd (project: Project)
                           (packageManager: Node.PackageManager)
                           =
    Cli.Wrap(baseCmd packageManager).WithArguments([| "install" |])
       .WithWorkingDirectory(project.Path)
       .WithEnvironmentVariables(addPathToCliWrapCmd)

  let getCliWrapCmd (targetPath: string)
                    (auCommand: AureliaCli.AuCliCommand)
                    : Command =

    let command = AuCliCommand.GenerateCommand auCommand

    Cli.Wrap(command.Command).WithArguments(command.Args)
       .WithWorkingDirectory(targetPath)
       .WithEnvironmentVariables(addPathToCliWrapCmd)
