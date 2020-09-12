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
      Bundler: Bundler
      Transpiler: Transpiler
      CssPreprocessor: CssPreprocessor
      UnitTestingFx: UnitTestingFx
      E2ETestingFx: Option<E2ETestingFx>
      SampleCode: SampleCode
      ProjectType: ProjectType

      (* GUI Specific stuff *)
      ErrorMessage: Option<string>
      Logs: Option<string>
      CommandSuccess: Option<bool>
      IsWaiting: bool
      IsWorking: bool
    }


  type private Msg =
    | SelectDirectory
    | GenerateProject
    | SetProjectType of ProjectType
    | SetName of Option<string>
    | SetBundler of Bundler
    | SetTranspiler of Transpiler
    | SetCssPreprocessor of CssPreprocessor
    | SetUnitTestingFx of UnitTestingFx
    | SetE2ETestingFx of Option<E2ETestingFx>
    | SetSampleCode of SampleCode
    | SelectDirectorySuccess of Option<string>
    | CommandSuccess
    | SelectDirectoryError of exn
    | UpdateCommandError of Option<string>
    | UpdateLog of Option<string>
    | SetLog of Option<string>
    | SetCommandError of Option<string>
    | SetIsWaiting of bool
    | SetIsWorking of bool

  let defaultPath () =
    System.IO.Path.Combine
      (Environment.GetFolderPath(Environment.SpecialFolder.Personal),
       "AureliaProjects")

  let private init () =

    {
      Name = Some "new-project"
      Directory = Some(defaultPath ())
      Bundler = Bundler.Dumber
      Transpiler = Transpiler.Babel
      CssPreprocessor = CssPreprocessor.Css
      UnitTestingFx = UnitTestingFx.Jest
      E2ETestingFx = None
      SampleCode = SampleCode.AppMin
      ProjectType = ProjectType.Babel
      (* GUI Specific Stuff *)
      ErrorMessage = None
      Logs = None
      CommandSuccess = None
      IsWaiting = false
      IsWorking = false
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
      dispatch (SetIsWaiting true)
      task {
        dispatch (SetIsWorking true)
        try
          let! cmdResult =
            cmd.WithStandardErrorPipe(PipeTarget.ToDelegate
                                        (Some >> UpdateCommandError >> dispatch))
               .WithStandardOutputPipe(PipeTarget.ToDelegate
                                         (Some >> UpdateLog >> dispatch))
               .ExecuteAsync()

          printfn "%A" cmdResult
          dispatch CommandSuccess |> ignore
        with ex ->
          dispatch (SetIsWaiting false)
          dispatch (SetIsWorking false)
          dispatch (UpdateCommandError(Some ex.Message))
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
    | CommandSuccess ->
        { state with
            CommandSuccess = Some true
        },
        Cmd.batch [
          Cmd.ofMsg (SetIsWaiting false)
          Cmd.ofMsg (SetIsWorking false)
          Cmd.ofMsg (SetCommandError None)
        ]
    | SetCommandError maybe ->
        { state with
            ErrorMessage = maybe
            CommandSuccess = Some false
        },
        Cmd.none
    | UpdateCommandError maybe ->
        let errs =
          match maybe, state.ErrorMessage with
          | Some errs, Some stateerrs -> Some(stateerrs + "\n" + errs)
          | Some errs, None -> Some errs
          | None, Some errs -> Some errs
          | _ -> None

        { state with ErrorMessage = errs }, Cmd.none
    | GenerateProject ->
        let template: Option<Template> =
          match state.ProjectType with
          | ProjectType.Custom ->
              Some
                {
                  Bundler = state.Bundler
                  Transpiler = state.Transpiler
                  CssPreprocessor = state.CssPreprocessor
                  UnitTestingFx = state.UnitTestingFx
                  E2ETesingFx = state.E2ETestingFx
                  SampleCode = state.SampleCode
                }
          | _ -> None

        let path =
          defaultArg state.Directory (defaultPath ())

        let name = defaultArg state.Name "new-project"

        let ensureDirectoryExists () =
          try
            Ok(IO.Directory.CreateDirectory(path) |> ignore)
          with ex -> Error ex.Message

        let elmishCmd =
          match CliInterop.newProjectCmd name (state.ProjectType, template) path with
          | Ok cmd ->
              match ensureDirectoryExists () with
              | Ok _ ->
                  Cmd.batch [
                    Cmd.ofMsg (SetIsWorking true)
                    Cmd.ofMsg (SetLog None)
                    Cmd.ofMsg (SetCommandError None)
                    Cmd.ofSub (onCommandExecution cmd)
                  ]
              | Error msg ->
                  Cmd.batch [
                    Cmd.ofMsg (SetIsWaiting false)
                    Cmd.ofMsg (SetIsWorking false)
                    Cmd.ofMsg (UpdateCommandError(Some msg))
                  ]
          | Error err ->
              Cmd.batch [
                Cmd.ofMsg (SetIsWaiting false)
                Cmd.ofMsg (SetIsWorking false)
                Cmd.ofMsg (UpdateCommandError(Some err))
              ]

        state, elmishCmd
    | SetIsWaiting waiting -> { state with IsWaiting = waiting }, Cmd.none
    | SetIsWorking working -> { state with IsWorking = working }, Cmd.none
    | SelectDirectory ->
        requestFolderDialog ()
        state, Cmd.ofMsg (SetIsWaiting true)
    | SetProjectType pType -> { state with ProjectType = pType }, Cmd.none
    | SetName name -> { state with Name = name }, Cmd.none
    | SetBundler bundler -> { state with Bundler = bundler }, Cmd.none
    | SetTranspiler transpiler ->
        { state with Transpiler = transpiler }, Cmd.none
    | SetCssPreprocessor csspp ->
        { state with CssPreprocessor = csspp }, Cmd.none
    | SetUnitTestingFx testingfx ->
        { state with UnitTestingFx = testingfx }, Cmd.none
    | SetE2ETestingFx e2etestingfx ->
        { state with
            E2ETestingFx = e2etestingfx
        },
        Cmd.none
    | SetSampleCode sampleCode ->
        { state with SampleCode = sampleCode }, Cmd.none
    | SelectDirectorySuccess path ->
        { state with
            Directory = path
            ErrorMessage = None
        },
        Cmd.ofMsg (SetIsWaiting false)
    | SelectDirectoryError exn ->
        { state with
            ErrorMessage = Some exn.Message
        },
        Cmd.ofMsg (SetIsWaiting false)

  let private nameAndPathSection (name: Option<string>)
                                 (path: Option<string>)
                                 (dispatch: Msg -> unit)
                                 =
    let name = defaultArg name "new-project"

    let path =
      let defaultPath =
        System.IO.Path.Combine
          (Environment.GetFolderPath(Environment.SpecialFolder.Personal),
           "AureliaProjects")

      defaultArg path defaultPath

    Common.FormSection [ AuGuiClassNames.NameAndPathSection ] [
      TextBlock.create [
        TextBlock.text "New Project Name"
      ]
      TextBox.create [
        TextBox.text name
        TextBox.horizontalAlignment HorizontalAlignment.Left
        TextBox.watermark "New Project Name"
        TextBox.onTextChanged (Option.ofObj >> SetName >> dispatch)
      ]
      TextBlock.create [
        TextBlock.text "Where is this project located at (parent folder)"
      ]
      StackPanel.create [
        StackPanel.orientation Orientation.Horizontal
        StackPanel.spacing 4.
        StackPanel.children [
          TextBox.create [
            TextBox.text path
            TextBox.watermark "Workspace Root"
            TextBox.isReadOnly true
          ]
          Button.create [
            Button.content "Select Workspace"
            Button.onClick (fun _ -> dispatch (SelectDirectory))
          ]
        ]
      ]
    ]

  let private templateSelectionSection (state: State) (dispatch: Dispatch<Msg>) =
    Common.RadioGroupStack<Msg>
      [
        AuGuiClassNames.TemplateSelectionSection
      ]
      [ AuGuiClassNames.HorizontalRadios ]
      "Template Type"
      [
        {|
          GroupName = "Template"
          Label = "Babel"
          IsChecked = (state.ProjectType = ProjectType.Babel)
          onChecked = SetProjectType ProjectType.Babel
        |}
        {|
          GroupName = "Template"
          Label = "Typescript"
          IsChecked = (state.ProjectType = ProjectType.Typescript)
          onChecked = SetProjectType ProjectType.Typescript
        |}
        {|
          GroupName = "Template"
          Label = "Custom"
          IsChecked = (state.ProjectType = ProjectType.Custom)
          onChecked = SetProjectType ProjectType.Custom
        |}
      ]
      dispatch


  let private customSection (state: State) (dispatch: Msg -> unit) =
    Common.WrappingFormSection [
                                 AuGuiClassNames.CustomTemplateSection
                               ] [
      Common.FormSection [ AuGuiClassNames.VerticalRadios ] [
        TextBlock.create [
          TextBlock.classes [
            AuGuiClassNames.RadioGroupLabel
          ]
          TextBlock.text "Bundler"
        ]
        yield!
          Common.RadiosContent<Msg>
            [
              {|
                GroupName = "Bundler"
                Label = "Dumber"
                IsChecked = state.Bundler = Bundler.Dumber
                onChecked = SetBundler Bundler.Dumber
              |}
              {|
                GroupName = "Bundler"
                Label = "Webpack"
                IsChecked = state.Bundler = Bundler.Webpack
                onChecked = SetBundler Bundler.Webpack
              |}
            ]
            dispatch
      ]
      Common.FormSection [ AuGuiClassNames.VerticalRadios ] [
        TextBlock.create [
          TextBlock.classes [
            AuGuiClassNames.RadioGroupLabel
          ]
          TextBlock.text "Transpiler"
        ]
        yield!
          Common.RadiosContent<Msg>
            [
              {|
                GroupName = "Transpiler"
                Label = "Babel"
                IsChecked = state.Transpiler = Transpiler.Babel
                onChecked = SetTranspiler Transpiler.Babel
              |}
              {|
                GroupName = "Transpiler"
                Label = "Typescript"
                IsChecked = state.Transpiler = Transpiler.Typescript
                onChecked = SetTranspiler Transpiler.Typescript
              |}
            ]
            dispatch
      ]
      Common.FormSection [ AuGuiClassNames.VerticalRadios ] [
        TextBlock.create [
          TextBlock.classes [
            AuGuiClassNames.RadioGroupLabel
          ]
          TextBlock.text "Css Preprocessor"
        ]
        yield!
          Common.RadiosContent<Msg>
            [
              {|
                GroupName = "CssPreprocessor"
                Label = "CSS"
                IsChecked = state.CssPreprocessor = CssPreprocessor.Css
                onChecked = SetCssPreprocessor CssPreprocessor.Css
              |}
              {|
                GroupName = "CssPreprocessor"
                Label = "Sass"
                IsChecked = state.CssPreprocessor = CssPreprocessor.Sass
                onChecked = SetCssPreprocessor CssPreprocessor.Sass
              |}
              {|
                GroupName = "CssPreprocessor"
                Label = "Less"
                IsChecked = state.CssPreprocessor = CssPreprocessor.Less
                onChecked = SetCssPreprocessor CssPreprocessor.Less
              |}
            ]
            dispatch
      ]
      Common.FormSection [ AuGuiClassNames.VerticalRadios ] [
        TextBlock.create [
          TextBlock.classes [
            AuGuiClassNames.RadioGroupLabel
          ]
          TextBlock.text "Unit Testing"
        ]
        yield!
          Common.RadiosContent<Msg>
            [
              {|
                GroupName = "UnitTestingFx"
                Label = "Jest"
                IsChecked = state.UnitTestingFx = UnitTestingFx.Jest
                onChecked = SetUnitTestingFx UnitTestingFx.Jest
              |}
              {|
                GroupName = "UnitTestingFx"
                Label = "Mocha"
                IsChecked = state.UnitTestingFx = UnitTestingFx.Mocha
                onChecked = SetUnitTestingFx UnitTestingFx.Mocha
              |}
              {|
                GroupName = "UnitTestingFx"
                Label = "Tape"
                IsChecked = state.UnitTestingFx = UnitTestingFx.Tape
                onChecked = SetUnitTestingFx UnitTestingFx.Tape
              |}
              {|
                GroupName = "UnitTestingFx"
                Label = "Jasmine"
                IsChecked = state.UnitTestingFx = UnitTestingFx.Jasmine
                onChecked = SetUnitTestingFx UnitTestingFx.Jasmine
              |}
            ]
            dispatch
      ]
      Common.FormSection [ AuGuiClassNames.VerticalRadios ] [
        TextBlock.create [
          TextBlock.classes [
            AuGuiClassNames.RadioGroupLabel
          ]
          TextBlock.text "E2E Testing"
        ]
        yield!
          Common.RadiosContent<Msg>
            [
              {|
                GroupName = "E2ETestingFx"
                Label = "None"
                IsChecked = state.E2ETestingFx = None
                onChecked = SetE2ETestingFx None
              |}
              {|
                GroupName = "E2ETestingFx"
                Label = "Cypress"
                IsChecked = state.E2ETestingFx = Some E2ETestingFx.Cypress
                onChecked = SetE2ETestingFx(Some E2ETestingFx.Cypress)
              |}
            ]
            dispatch
      ]
      Common.FormSection [ AuGuiClassNames.VerticalRadios ] [
        TextBlock.create [
          TextBlock.classes [
            AuGuiClassNames.RadioGroupLabel
          ]
          TextBlock.text "Sample Code"
        ]
        yield!
          Common.RadiosContent<Msg>
            [
              {|
                GroupName = "SampleCode"
                Label = "Minimum App"
                IsChecked = state.SampleCode = SampleCode.AppMin
                onChecked = SetSampleCode SampleCode.AppMin
              |}
              {|
                GroupName = "SampleCode"
                Label = "App with direct routing"
                IsChecked = state.SampleCode = SampleCode.AppMin
                onChecked = SetSampleCode SampleCode.AppWithRouter
              |}
            ]
            dispatch
      ]
    ]


  let private footer (state: State) (dispatch: Dispatch<Msg>) =
    Common.FormSection [ AuGuiClassNames.FooterSection ] [
      Button.create [
        Button.content "Generate Project"
        Button.horizontalAlignment HorizontalAlignment.Left
        Button.isEnabled ((not state.IsWaiting) && not state.IsWorking)
        Button.onClick (fun _ -> dispatch (GenerateProject))
      ]
      if state.IsWaiting || state.IsWorking then
        ProgressBar.create [
          ProgressBar.isIndeterminate true
        ]
      if state.Logs <> None || state.ErrorMessage <> None then
        Expander.create [
          Expander.classes [
            AuGuiClassNames.LogExpander
          ]
          Expander.header "Check Logs"
          Expander.content
            (ScrollViewer.create [
              ScrollViewer.classes [
                AuGuiClassNames.LogScrollViewer
              ]
              ScrollViewer.content
                (DockPanel.create [
                  DockPanel.lastChildFill false
                  DockPanel.children [
                    TextBlock.create [
                      TextBlock.classes [
                        AuGuiClassNames.LogTextBlock
                      ]
                      TextBlock.dock Dock.Left
                      TextBlock.text (state.Logs |> Option.defaultValue "")
                    ]
                    TextBlock.create [
                      TextBlock.classes [
                        AuGuiClassNames.LogTextBlock
                        AuGuiClassNames.ErrorLogTextBlock
                      ]
                      TextBlock.dock Dock.Right
                      TextBlock.text
                        (state.ErrorMessage |> Option.defaultValue "")
                    ]
                  ]
                 ])
             ])
        ]
    ]


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
          nameAndPathSection state.Name state.Directory dispatch
          if state.ProjectType = ProjectType.Custom then
            templateSelectionSection state dispatch
            customSection state dispatch
            footer state dispatch
          else
            templateSelectionSection state dispatch
            footer state dispatch
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
