namespace AuGui

open System
open Avalonia.FuncUI.Components.Hosts
open FSharp.Control.Tasks
open AuGui.Core
open AuGui.Components
open Avalonia.FuncUI.Types
open Avalonia.FuncUI.Helpers
open System.IO
open Avalonia.Controls.Primitives
open Avalonia.Input

module Workspace =
  open Elmish
  open Avalonia.Controls
  open Avalonia.Layout
  open Avalonia.FuncUI.DSL
  open Avalonia.FuncUI.Elmish

  type private State =
    {
      Workplace: Workplace
      (* UI Props *)
      IsWorking: bool
      EnsureDirError: Option<string>
      PopulateError: Option<exn>
    }


  type private Msg =
    | PopulateWorkplace
    | SelectProject of Project
    | EnsureWorkplaceExists of Result<DirectoryInfo, exn>
    | AddProjects of array<Result<FileInfo * Node.PackageJson, string>>
    (* UI Prop Msgs *)
    | SetIsWorking of bool
    | SetEnsureDirError of Option<string>
    | SetPopulateError of Option<exn>

  let private init () =
    let state: State =
      {
        Workplace =
          {
            Name = "default"
            Path = Workspace.GetDefaultWorkspacePath()
            Projects = Seq.empty<Project>
          }
        IsWorking = false
        EnsureDirError = None
        PopulateError = None
      }

    let ensureResult =
      Workspace.EnsureWorkspacePathExists(state.Workplace.Path)

    state, Cmd.ofMsg (EnsureWorkplaceExists ensureResult)



  let private update (msg: Msg)
                     (state: State)
                     (requestPageChange: Page -> unit)
                     : State * Cmd<Msg> =
    match msg with
    | PopulateWorkplace ->
        let asyncCmd () =
          async {
            let! result = Workspace.DetectAureliaProjects(state.Workplace.Path)
            return result
          }

        state,
        Cmd.batch [
          Cmd.ofMsg (SetIsWorking true)
          Cmd.OfAsync.either
            asyncCmd
            ()
            AddProjects
            (Option.ofObj >> SetPopulateError)
        ]
    | SelectProject project ->
        requestPageChange (Page.Project project)
        state, Cmd.none
    | EnsureWorkplaceExists result ->
        let cmd =
          match result with
          | Ok dir ->
              Cmd.batch [
                Cmd.ofMsg (SetEnsureDirError None)
                Cmd.ofMsg PopulateWorkplace
              ]
          | Error ex -> Cmd.ofMsg (SetEnsureDirError(Some ex.Message))

        state, cmd
    | AddProjects projects ->
        let okProjects, errorProjects =
          projects
          |> Array.Parallel.partition (fun result ->
               match result with
               | Ok _ -> true
               | Error _ -> false)

        errorProjects
        |> Array.Parallel.iter (fun error ->
             match error with
             | Error error -> printfn $"Failed to parse package.json : {error}"
             | Ok _ -> ())

        let projects =
          okProjects
          |> Array.Parallel.choose (fun ok ->
               match ok with
               | Ok (file, packagejson) ->
                   Some
                     {
                       Directory = file.Directory.FullName
                       PackageJson = packagejson
                       Path = file.FullName
                     }
               | _ -> None)

        { state with
            Workplace =
              { state.Workplace with
                  Projects = projects
              }
        },
        Cmd.ofMsg (SetIsWorking false)
    (* GUI Msgs *)
    | SetEnsureDirError err -> { state with EnsureDirError = err }, Cmd.none
    | SetPopulateError err -> { state with PopulateError = err }, Cmd.none
    | SetIsWorking isWorking -> { state with IsWorking = isWorking }, Cmd.none


  let private projectGrid (state: State) (dispatch: Dispatch<Msg>) =
    UniformGrid.create [
      UniformGrid.children [
        for project in state.Workplace.Projects do
          Common.FormSection [] [
            Border.create [
              Border.background "#F0F0F0"
              Border.borderBrush "#CCCCCC"
              Border.borderThickness 1.5
              Border.cornerRadius 4.
              Border.cursor (Cursor(StandardCursorType.Hand))
              Border.onTapped (fun _ -> dispatch (SelectProject project))
              Border.child
                (TextBlock.create [
                  TextBlock.text project.PackageJson.name
                  TextBlock.margin 4.0
                  TextBlock.foreground "black"
                 ])
            ]
          ]
      ]
    ]


  let private view (state: State) (dispatch: Dispatch<Msg>): IView =
    DockPanel.create [
      DockPanel.children [
        Common.FormSection [] [
          TextBlock.create [
            TextBlock.text state.Workplace.Name
          ]
          TextBlock.create [
            TextBlock.text state.Workplace.Path
          ]
        ]
        ScrollViewer.create [
          ScrollViewer.dock Dock.Top
          ScrollViewer.content (projectGrid state dispatch)
        ]
      ]
    ]
    |> generalize

  type Host() as this =
    inherit HostControl()

    let updateWithServices msg state =
      update msg state (AuGuiEvents.RequestPageChange)

    do
      Program.mkProgram init updateWithServices view
      |> Program.withHost this
#if DEBUG
      |> Program.withConsoleTrace
#endif
      |> Program.run
