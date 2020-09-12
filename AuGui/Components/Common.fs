namespace AuGui.Components

open Elmish
open Avalonia.Layout
open Avalonia.Controls
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open AuGui
open Avalonia.Media

[<RequireQualifiedAccess>]
module Common =
  type RadioGroupWithAction<'msg> =
    {| Label: string
       GroupName: string
       IsChecked: bool
       onChecked: 'msg |}

  let FormSection (classes: list<string>) (children: list<IView>) =
    StackPanel.create [
      StackPanel.classes [
        AuGuiClassNames.FormSection
        yield! classes
      ]
      StackPanel.children children
    ]

  let WrappingFormSection (classes: list<string>) (children: list<IView>) =
    WrapPanel.create [
      WrapPanel.classes [
        AuGuiClassNames.WrappingFormSection
        yield! classes
      ]
      WrapPanel.children children
    ]

  let RadiosContent<'msg> (options: list<{| Label: string
                                            GroupName: string
                                            IsChecked: bool
                                            onChecked: 'msg |}>)
                          (dispatch: Dispatch<'msg>)
                          : list<IView> =
    [
      for radio in options do
        RadioButton.create [
          RadioButton.groupName radio.GroupName
          RadioButton.content radio.Label
          RadioButton.isChecked radio.IsChecked
          RadioButton.onChecked ((fun _ -> dispatch (radio.onChecked)), Always)
        ]
    ]

  let RadioGroupStack<'msg> (classes: list<string>)
                            (radioClasses: list<string>)
                            (radioGroupLabel: string)
                            (options: list<RadioGroupWithAction<'msg>>)
                            (dispatch: Dispatch<'msg>)
                            =
    FormSection [ yield! classes ] [
      TextBlock.create [
        TextBlock.classes [
          AuGuiClassNames.RadioGroupLabel
        ]
        TextBlock.text radioGroupLabel
      ]
      StackPanel.create [
        StackPanel.classes [
          AuGuiClassNames.RadioGroup
          yield! radioClasses
        ]
        StackPanel.children [
          yield! RadiosContent options dispatch
        ]
      ]
    ]

  let WrappingRadioGroup<'msg> (classes: list<string>)
                               (orientation: Option<Orientation>)
                               (radioGroupLabel: string)
                               (options: list<RadioGroupWithAction<'msg>>)
                               (dispatch: Dispatch<'msg>)
                               =
    let orientation =
      defaultArg orientation Orientation.Vertical

    WrappingFormSection [
                          AuGuiClassNames.WrappingRadioGroup
                          yield! classes
                        ] [
      StackPanel.create [
        StackPanel.classes [
          AuGuiClassNames.RadioGroup
          yield! classes
        ]
        StackPanel.orientation orientation
        StackPanel.children [
          TextBlock.create [
            TextBlock.classes [
              AuGuiClassNames.RadioGroupLabel
            ]
            TextBlock.text radioGroupLabel
          ]
          yield! RadiosContent options dispatch
        ]
      ]
    ]


  let DockForm (classes: list<string>) (children: list<IView>) =
    DockPanel.create [
      DockPanel.classes [
        AuGuiClassNames.DockForm
        yield! classes
      ]
      DockPanel.children children
    ]
