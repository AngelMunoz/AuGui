namespace AuGui

open Avalonia.Media
open Avalonia.FuncUI.Components.Hosts
open System.Threading.Tasks
open FSharp.Control.Tasks
open CliWrap
open CliWrap.EventStream
open AuGui.Core
open AuGui.Components
open System
open Avalonia.FuncUI.Types
open Avalonia.FuncUI.Helpers

module NewProject =
  open Elmish
  open Avalonia.Controls
  open Avalonia.Layout
  open Avalonia.FuncUI.DSL
  open Avalonia.FuncUI.Elmish

  type private State =
    {
      Name: Option<string>
      Directory: Option<string>
      Logs: Option<string>
      DialogError: Option<string>
      CommandError: Option<string>
    }


  type private Msg =
    | SelectDirectory
    | GenerateProject
    | CommandSuccess
    | SelectDirectorySuccess of Option<string>
    | SelectDirectoryError of exn
    | SetCommandError of Option<string>
    | UpdateCommandError of Option<string>
    | SetLog of Option<string>
    | UpdateLog of Option<string>
    | SetName of Option<string>

  let defaultPath () =
    System.IO.Path.Combine
      (Environment.GetFolderPath(Environment.SpecialFolder.Personal),
       "AureliaProjects")

  let private init () =

    {
      Name = Some "new-project"
      Directory = Some(defaultPath ())
      Logs = None
      CommandError = None
      DialogError = None
    },
    Cmd.none


  let private onFolderDialogSuccess (_: State) =
    let sub dispatch =
      AuGuiEvents.OnRequestFolderDialogSuccess.Subscribe
        (SelectDirectorySuccess >> dispatch)
      |> ignore

    Cmd.ofSub sub

  let private onFolderDialogError (_: State) =
    let sub dispatch =
      AuGuiEvents.OnRequestFolderDialogError.Subscribe
        (SelectDirectoryError >> dispatch)
      |> ignore

    Cmd.ofSub sub

  let private onCommandExecution (cmd: Command): Sub<Msg> =
    let sub (dispatch: Dispatch<Msg>) =
      task {
        try
          let! cmdResult =
            cmd.WithStandardErrorPipe(PipeTarget.ToDelegate
                                        (Some >> UpdateCommandError >> dispatch))
               .WithStandardOutputPipe(PipeTarget.ToDelegate
                                         (Some >> UpdateLog >> dispatch))
               .ExecuteAsync()

          printfn "%A" cmdResult

          dispatch CommandSuccess |> ignore
        with ex -> dispatch (UpdateCommandError(Some ex.Message))
      }
      |> Async.AwaitTask
      |> Async.StartImmediate

    sub



  let private update (msg: Msg)
                     (state: State)
                     (requestFolderDialog: unit -> unit)
                     =
    match msg with
    | SetLog maybe -> { state with Logs = maybe }, Cmd.none
    | UpdateLog maybe ->
        let logs =
          match maybe, state.Logs with
          | Some logs, Some stateLogs -> Some(stateLogs + "\n" + logs)
          | Some logs, None -> Some logs
          | None, Some logs -> Some logs
          | _ -> None

        { state with Logs = logs }, Cmd.none
    | CommandSuccess -> state, Cmd.ofMsg (SetCommandError None)
    | SetCommandError maybe -> { state with CommandError = maybe }, Cmd.none
    | UpdateCommandError maybe ->
        let errs =
          match maybe, state.CommandError with
          | Some errs, Some stateerrs -> Some(stateerrs + "\n" + errs)
          | Some errs, None -> Some errs
          | None, Some errs -> Some errs
          | _ -> None

        { state with CommandError = errs }, Cmd.none
    | GenerateProject -> state, Cmd.none
    | SelectDirectory ->
        requestFolderDialog ()
        state, Cmd.none
    | SelectDirectorySuccess path ->
        { state with
            Directory = path
            DialogError = None
        },
        Cmd.none
    | SelectDirectoryError exn ->
        { state with
            DialogError = Some exn.Message
        },
        Cmd.none
    | SetName name -> { state with Name = name }, Cmd.none


  let private view (state: State) (dispatch: Msg -> unit): IView =
    ScrollViewer.create [
      ScrollViewer.content
        (Common.DockForm [ AuGuiClassNames.NewProjectForm ] [
          Common.FormSection [ AuGuiClassNames.DockTop ] [
            TextBlock.create [
              TextBlock.classes [
                AuGuiClassNames.ViewHeader
              ]
              TextBlock.text "New Project"
            ]
          ]
         ])
    ]
    |> generalize

  type Host() as this =
    inherit HostControl()

    let updateWithServices msg state =
      update msg state (AuGuiEvents.RequestFolderDialog)

    do
      Program.mkProgram init updateWithServices view
      |> Program.withHost this
      |> Program.withSubscription onFolderDialogSuccess
      |> Program.withSubscription onFolderDialogError
#if DEBUG
      |> Program.withConsoleTrace
#endif
      |> Program.run
