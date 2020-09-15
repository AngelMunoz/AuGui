namespace AuGui

open Avalonia.FuncUI.Components
open Avalonia.Layout
open Avalonia.Controls.Primitives
open AuGui.Core
open FSharp.Control.Tasks

/// This is the main module of your application
/// here you handle all of your child pages as well as their
/// messages and their updates, useful to update multiple parts
/// of your application, Please refer to the `view` function
/// to see how to handle different kinds of "*child*" controls
module Shell =
  open Elmish
  open Avalonia
  open Avalonia.Controls
  open Avalonia.Input
  open Avalonia.FuncUI
  open Avalonia.FuncUI.Builder
  open Avalonia.FuncUI.Components.Hosts
  open Avalonia.FuncUI.DSL
  open Avalonia.FuncUI.Elmish
  open AuGui.Core

  type State = { Page: Page }

  type Msg =
    | ChangePage of Page
    | OpenDirectoryDialog of unit
    | OpenDirectoryDialogSuccess of Result<Option<string>, exn>
    | OpenDirectoryDialogError of exn

  let init () = { Page = Page.Home }, Cmd.none

  let onRequestFolderDialog (_: State) =
    let sub dispatch =
      AuGuiEvents.OnRequestFolderDialog.Subscribe
        (OpenDirectoryDialog >> dispatch)
      |> ignore

    Cmd.ofSub sub

  let onRequestPageChange (_: State) =
    let sub dispatch =
      AuGuiEvents.OnRequestPageChange.Subscribe(ChangePage >> dispatch)
      |> ignore

    Cmd.ofSub sub


  let update (msg: Msg)
             (state: State)
             (window: Window)
             (dialogSuccess: Option<string> -> unit)
             (dialogError: exn -> unit)
             : State * Cmd<Msg> =
    match msg with
    | ChangePage page -> { state with Page = page }, Cmd.none
    | OpenDirectoryDialog _ ->
        let openFolderDialog () =
          task {
            let dialog = OpenFolderDialog()
            try
              let! path = dialog.ShowAsync(window)
              return Ok(Option.ofObj path)
            with ex ->
              eprintfn "%O" ex
              return Error ex
          }

        state,
        Cmd.OfTask.either
          openFolderDialog
          ()
          OpenDirectoryDialogSuccess
          OpenDirectoryDialogError
    | OpenDirectoryDialogSuccess path ->
        match path with
        | Ok path ->
            dialogSuccess path
            state, Cmd.none
        | Error exn -> state, Cmd.ofMsg (OpenDirectoryDialogError exn)
    | OpenDirectoryDialogError ex ->
        dialogError ex
        state, Cmd.none


  let view (state: State) (dispatch) =
    DockPanel.create [
      DockPanel.children [
        TabControl.create [
          TabControl.dock Dock.Top
          TabControl.tabStripPlacement Dock.Left
          TabControl.verticalScrollBarVisibility ScrollBarVisibility.Visible
          TabControl.viewItems [
            TabItem.create [
              TabItem.header "New Project"
              TabItem.content (ViewBuilder.Create<NewProject.Host>([]))
            ]
            TabItem.create [
              TabItem.header "Workspace"
              TabItem.content (ViewBuilder.Create<Workspace.Host>([]))
            ]
          ]
        ]
      ]
    ]

  /// This is the main window of your application
  /// you can do all sort of useful things here like setting heights and widths
  /// as well as attaching your dev tools that can be super useful when developing with
  /// Avalonia
  type MainWindow() as this =
    inherit HostWindow()

    do
      base.Title <- "AuGui"
      base.Width <- 800.0
      base.Height <- 600.0
      base.MinWidth <- 800.0
      base.MinHeight <- 600.0

      //this.VisualRoot.VisualRoot.Renderer.DrawFps <- true
      //this.VisualRoot.VisualRoot.Renderer.DrawDirtyRects <- true

#if DEBUG
      this.AttachDevTools()
#endif

      let updateWithServices msg state =
        update
          msg
          state
          this
          AuGuiEvents.RequestFolderDialogSuccess
          AuGuiEvents.RequestFolderDialogError

      Elmish.Program.mkProgram init updateWithServices view
      |> Program.withHost this
      |> Program.withSubscription onRequestFolderDialog
      |> Program.withSubscription onRequestPageChange
#if DEBUG
      |> Program.withConsoleTrace
#endif
      |> Program.run
