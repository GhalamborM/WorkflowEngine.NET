var WorkflowDesignerConstants = {
  ActivityColor: "#ECF0F1",
  ActivityTextColor: "#2D3436",
  ActivityInitialColor: "#27AE60",
  ActivityInitialTextColor: "#FFFFFF",
  ActivityFinalColor: "#2980B9",
  ActivityFinalTextColor: "#FFFFFF",
  ActivityShape: "#CECECE",
  SelectColor: "#F39C12",
  SelectTextColor: "#FFFFFF",
  SelectSubProcessColor: "#e3b015",
  SelectSubProcessTextColor: "#FFFFFF",
  ButtonActive: "#D4D8D9",
  DateTimeFormat: "YYYY-MM-DD HH:MM:SS",
  BarColor: "#EDF1F2",
  BarSeparatorColor: "#D9DEE0",
  IsRequiredColor: "#F56C6C",
  ParameterFromProcessColor: "#1f8dd6",
  DeleteConfirm: "Êtes-vous sûr de vouloir supprimer le ou les éléments sélectionnés?",
  DeleteConfirmCurrent: "Êtes-vous sûr de bien vouloir supprimer cet élément?",
  FieldIsRequired: "Champ requis!",
  FieldIsNotCorrected: "Field is not corrected!",
  FieldMustBeUnique: "Le champ doit être unique!",
  FieldMustContainAt: "Field must contain @",
  ButtonTextDelete: "Effacer",
  ButtonTextCreate: "Créer",
  ButtonTextSave: "Sauver",
  ButtonTextYes: "Oui",
  ButtonTextNo: "Non",
  ButtonTextOk: "Ok",
  ButtonTextCancel: "Annuler",
  ButtonTextClose: "Fermer",
  ButtonTextUndo: "annuler",
  ButtonTextRedo: "Refaire",
  SaveConfirm: "Sauver les modifications?",
  CloseWithoutSaving: "Fermer sans enregistrer?",
  TransitionAuto: "Auto",
  DialogConfirmText: "Question",
  AlertTitle: "Warning",
  None: "Aucun",
  Warning: "Attention",
  SwitchToExpertMode: "Switch To Expert Mode",
  SwitchToDefaultMode: "Switch To Default Mode",
  InfoBlockLabel: {
    Activity: "Activités:",
    Transition: "Transitions:",
    Command: "Commandes:"
  },
  ActivityNamePrefix: "Activité_",
  ActivityFormLabel: {
    Title: "Activité",
    TitleForInline: "Activity Inline",
    Name: "Prénom",
    State: "Etat",
    IsInitial: "Initiale",
    IsFinal: "Final",
    IsForSetState: "Pour l'état défini",
    IsAutoSchemeUpdate: "Mise à jour automatique",
    DisablePersist: "Disable persist",
    DisablePersists: {
      DisablePersistTransitionHistory: "Disable persist transition history",
      DisablePersistState: "Disable persist process state",
      DisablePersistParameters: "Disable persist process parameters"
    },
    Implementation: "La mise en oeuvre",
    PreExecutionImplementation: "La mise en oeuvre PreExecution",
    ImpOrder: "Ordre",
    ExecutionTimeoutLabel: "Execution timeout",
    ExecutionTimeoutWarning: "Execution timeout works only with async actions. Check the cancellation token for proper timeout handling.",
    TimeoutsLabel: "Timeouts",
    TypeLabel: "Type",
    ActivityLabel: "Activity",
    StateLabel: "State",
    RetryCountLabel: "Retry count",
    SetActivityLabel: "SetActivity",
    SetStateLabel: "SetState",
    RetryLabel: "Retry",
    IgnoreLabel: "Ignore",
    AllExceptions: "*",
    IdleTimeoutLabel: "Idle timeout",
    ExceptionsHandling: "Exceptions handling",
    Exceptions: "Exceptions",
    ImpAction: "Action",
    ImpActionParameter: "Paramètre d'action",
    Annotations: "Annotations",
    AnnotationName: "Name",
    AnnotationValue: "Value",
    Scheme: "Inline scheme",
    UserComment: "Commenter",
    AlwaysConditionShouldBeSingle: "Toujours condition devrait être célibataire",
    OtherwiseConditionShouldBeSingle: "Sinon, la condition devrait être unique"
  },
  Decision: {
    Title: "Decision",
    TitleForInline: "Decision Inline",
    To: "To activity",
    WhenTrue: "When True",
    WhenFalse: "When False",
    Transition: "Transitions to activities",
    Name: "Name",
    State: "State",
    IsInitial: "Initial",
    IsFinal: "Final",
    IsForSetState: "For set state",
    IsAutoSchemeUpdate: "Auto scheme update",
    Implementation: "Implementation",
    PreExecutionImplementation: "PreExecution Implementation",
    Annotations: "Annotations",
    Scheme: "Inline scheme",
    Condition: "Condition",
    ConditionType: "Type",
    ConditionAction: "Action",
    ResultOnPreExecution: "PreExec. Result",
    ConditionActionParameter: "Action parameter",
    ConditionInversion: "Invert result",
    ActionLabel: "Action",
    ExpressionLabel: "Expression",
    UserComment: "Comment"
  },
  DecisionTable: {
    Title: "Decision table",
    TitleForInline: "Decision table Inline",
    To: "To activity",
    Transition: "Transitions to activities",
    Parameters: "Parameters",
    Name: "Name",
    State: "State",
    IsInitial: "Initial",
    IsFinal: "Final",
    IsForSetState: "For set state",
    IsAutoSchemeUpdate: "Auto scheme update",
    Implementation: "Implementation",
    PreExecutionImplementation: "PreExecution Implementation",
    Annotations: "Annotations",
    Scheme: "Inline scheme",
    ConditionType: "Type",
    Condition: "Condition",
    ConditionAction: "Action",
    ResultOnPreExecution: "PreExec. Result",
    ConditionActionParameter: "Action parameter",
    ConditionInversion: "Invert result",
    ActionLabel: "Action",
    ExpressionLabel: "Expression",
    UserComment: "Comment"
  },
  TransitionFormLabel: {
    Title: "Transition",
    Name: "Prénom",
    From: "De l'activité",
    To: "À l'activité",
    Classifier: "Classificateur",
    ClassifierValues: ["NotSpecified", "Direct", "Reverse"],
    Restrictions: "Restrictions",
    RestrictionsType: "Type",
    RestrictionsActor: "Acteur",
    Condition: "État",
    ConditionType: "Type",
    ConditionAction: "action",
    ResultOnPreExecution: "Résultat sur la pré-exécution",
    Trigger: "Déclencheur",
    TriggerType: "Type",
    TriggerAuto: "Auto",
    TriggerCommand: "Commander",
    TriggerTimer: "Minuteur",
    ConditionActionParameter: "Paramètre d'action",
    ConditionExpression: "Expression",
    ConditionInversion: "Inverser le résultat de l'action",
    ConditionsConcatenationType: "Type de concaténation des conditions",
    AllowConcatenationType: "Concat permettent comme",
    RestrictConcatenationType: "Concat restreindre comme",
    IsFork: "Est la fourche",
    MergeViaSetState: "Fusionner le sous-processus via l'état défini",
    DisableParentStateControl: "Désactiver le contrôle de processus parent",
    ShowConcatParameters: "Afficher la concaténation",
    HideConcatParameters: "Masquer la concaténation",
    Annotations: "Annotations",
    AnnotationName: "Name",
    AnnotationValue: "Value",
    InlinedFinalActivityName: "Inlined Final Activity Name",
    Allow: "Allow",
    Restrict: "Restrict",
    ConditionsListShouldNotBeEmpty: "La liste des conditions ne doit pas être vide",
    AlwaysConditionShouldBeSingle: "Always condition should be single",
    OtherwiseConditionShouldBeSingle: "Otherwise condition should be single",
    ConditionalLabel: "Conditional",
    AlwaysLabel: "Always",
    ActionLabel: "Action",
    ExpressionLabel: "Expression",
    OtherwiseLabel: "Otherwise",
    UserComment: "Commenter",
    Subprocess: "Subprocess",
    SubprocessNone: "None",
    SubprocessStart: "Start",
    SubprocessFinalize: "Finalize",
    SubprocessAuto: "Auto",
    SubprocessStartSettings: "Subprocess start settings",
    SubprocessFinalizeSettings: "Subprocess finalize settings",
    SubprocessName: "Subprocess name",
    SubprocessId: "Subprocess id",
    SubprocessIdPlaceholder: "Autogenerated GUID",
    SubprocessNamePlaceholder: "Same as transition name",
    SubprocessStartupType: "Subprocess startup type",
    SubprocessStartupTypeSameThread: "Same thread",
    SubprocessStartupTypeAnotherThread: "Another thread",
    SubprocessStartupTypeTimerQueue: "Timer queue",
    SubprocessStartupParameterCopyStrategy: "Parameters copy strategy",
    SubprocessStartupParameterCopyStrategyCopyAll: "Copy all",
    SubprocessStartupParameterCopyStrategyIgnoreAll: "Ignore all",
    SubprocessStartupParameterCopyStrategyCopySpecified: "Copy specified",
    SubprocessStartupParameterCopyStrategyIgnoreSpecified: "Ignore specified",
    SubprocessSpecifiedParameters: "Specified parameters",
    SubprocessFinalizeParameterMergeStrategy: "Parameters merge strategy",
    SubprocessFinalizeParameterMergeStrategyOverwriteAllNulls: "Overwrite all nulls",
    SubprocessFinalizeParameterMergeStrategyOverwriteAll: "Overwrite all",
    SubprocessFinalizeParameterMergeStrategyOverwriteSpecified: "Overwrite specified",
    SubprocessFinalizeParameterMergeStrategyDontOverwriteSpecified: "Don't overwrite specified"
  },
  LocalizationFormLabel: {
    Title: "Localisation",
    ObjectName: "ObjectName",
    Type: "Type",
    IsDefault: "IsDefault",
    Culture: "Culture",
    Value: "Valeur",
    Types: ["Command", "State", "Parameter"]
  },
  TimerFormLabel: {
    Title: "Minuteries",
    Name: "Prénom",
    Type: "Type",
    Value: "Valeur",
    Types: ["Command", "State", "Parameter"],
    OverrideHint: "Écraser le timer s'il existe",
    Override: "Écraser"
  },
  AssignmentFormLabel: {
    Title: "Assignments",
    Code: "Code",
    Name: "Name",
    Executor: "Executor",
    ExecutorId: "Executor Id",
    Status: "Status",
    DateCreation: "Date creation",
    Description: "Description",
    DateStart: "Date start",
    DateFinish: "Date finish",
    DeadlineToStart: "Deadline to start",
    DeadlineToFinish: "Deadline to finish",
    Observers: "Observers",
    ObserverId: "Observer Id",
    Tags: "Tags",
    ProhibitedForExecution: "Prohibited for execution",
    AssignmentDeleted: "Assignment deleted",
    TypeSomething: "Type something"
  },
  ParameterFormLabel: {
    Title: "Paramètres",
    Name: "Prénom",
    Type: "Type",
    Purpose: "Objectif",
    Value: "Valeur",
    InitialValue: "Valeur initiale",
    ShowSystemParameters: "Afficher les paramètres du système"
  },
  ActorFormLabel: {
    Title: "Acteurs",
    Name: "Prénom",
    Rule: "Règle",
    Value: "Valeur",
    ShowPredefinedActors: "Show predefined actors"
  },
  CommandFormLabel: {
    Title: "Commander",
    Name: "Prénom",
    InputParameters: "Paramètres d'entrée",
    InputParametersName: "Prénom",
    InputParametersIsRequired: "Champs obligatoires",
    InputParametersParameter: "Paramètre",
    InputParametersDefaultValue: "Défaut"
  },
  CommandParametersFormLabel: {
    Title: "command parameters",
    InputParametersName: "Name",
    InputParametersIsRequired: "Required",
    InputParametersParameter: "Parameter",
    InputParametersDefaultValue: "Default"
  },
  ProcessInfoFormLabel: {
    Title: "Additional Parameters",
    IsObsolete: "IsObsolete",
    DefiningParameters: "Defining parameters",
    ProcessParameters: "Process parameters",
    SystemParametersTabName: "System Parameters",
    ProcessParametersTabName: "Process Parameters",
    HistoryTabName: "History",
    TimersTabName: "Timers",
    TagsTabName: "Tags",
    TagsInputPlaceholder: "Start entering a name to add a tag",
    HistoryTabFromLabel: "From",
    HistoryTabFromStateLabel: "From State",
    HistoryTabToLabel: "To",
    HistoryTabToStateLabel: "To State",
    HistoryTabExecutorIdLabel: "Executor Id",
    HistoryTabActorIdLabel: "Actor Id",
    HistoryTabExecutorLabel: "Executor",
    HistoryTabActorLabel: "Actor",
    HistoryTabTimeLabel: "Time",
    HistoryTabStartTimeLabel: "Start Time",
    HistoryTabDurationLabel: "Duration (ms)",
    HistoryTabTriggerNameLabel: "Trigger Name",
    HistoryTabTransitionClassifierNameLabel: "",
    ParametersNameLabel: "Name",
    ParametersValueLabel: "Value",
    TimersTabNameLabel: "Name",
    TimersTabValueLabel: "Value",
    RootProcess: "Root Process",
    TagsLabel: "Tags (separated by commas)",
    GeneralTabLabel: "General",
    ProcessIdLabel: "Process Id"
  },
  DropdownValues: {
    Name: "Name",
    Value: "Value",
    Dropdown: "Dropdown",
    Values: "values"
  },
  CodeActionsFormLabel: {
    Title: "Actions de code",
    Name: "Prénom",
    ActionCode: "Code d'action",
    CommonUsings: "Common Usings",
    IsGlobal: "Est globale",
    IsAsync: "Async",
    Type: "Type",
    GlobalDeleteMessage: "Si vous supprimez Global CodeAction - <br/> <b> les autres schémas ne pourront pas l'appeler! <\/b>",
    UnGlobalMessage: "Vous avez modifié l'état de l'indicateur global. <br/> Une action Code Local sera créée à partir de cette Action Code Global après avoir enregistré ce schéma.",
    EditParameters: "Edit parameters",
    Parameters: "Parameters",
    Text: "Text",
    TextArea: "TextArea",
    Number: "Number",
    Checkbox: "Checkbox",
    Dropdown: "Dropdown",
    MultiSelect: "MultiSelect",
    DateTime: "Date/Time",
    Date: "Date",
    Time: "Time",
    Values: "Values",
    DropdownName: "Name",
    DropdownValue: "Value",
    DropdownShouldContainValues: "Dropdown should contain values",
    IsRequired: "Required",
    DefaultValue: "Default value",
    Json: "Json",
    TitleField: "Title"
  },
  EditParametersFormlabel: {
    Title: "Edit parameter values",
    NumberRequired: "Should be a numeric value",
    DateShouldBeInISOFormat: "Date/Time parameter should be in ISO8601 format",
    SwitchToJson: "Switch to JSON editor",
    SwitchToConstructor: "Switch to parameter values editor",
    InvalidJson: "JSON object is invalid or does not match the model",
    CustomForms: {
      HTTPRequest: {
        Name: "Name",
        State: "State",
        UseAuth: "Use authentication",
        Heading: "Request",
        Url: "Url",
        Post: "Post",
        StoreResponse: "Store response",
        Username: "Username",
        Password: "Password",
        ParameterName: "Parameter name",
        ParameterPurpose: "Parameter purpose",
        ContentType: "Content type",
        AddProcessInstanceParameters: "Add process instance parameters",
        Parameters: "Parameters",
        Headers: "Headers"
      },
      SendEmail: {
        Name: "Name",
        State: "State",
        Heading: "Form",
        UseAuth: "Use authentication",
        MailServer: "Mail server",
        MailServerPort: "Mail server port",
        MailServerFrom: "Mail server from",
        To: "To",
        CcList: "CC address list",
        BccList: "BCC address list",
        ReplyToList: "Reply to address list",
        Subject: "Subject",
        MailServerSsl: "MailServerSsl",
        IsHTML: "IsHTML",
        MailServerLogin: "Mail server login",
        MailServerPass: "Mail server pass",
        Body: "Body"
      },
      StartLoopFor: {
        Name: "Name",
        State: "State",
        Heading: "Loop",
        DateRange: "Date range",
        RangeSeparator: "To",
        StartDate: "Start date",
        EndDate: "End date",
        Reverse: "Reverse",
        LoopName: "Loop name",
        LoopStateParameterName: "Loop state parameter name",
        LoopCounterValueParameterName: "Loop counter value parameter name",
        CounterType: "Counter type",
        StartValue: "Start value",
        EndValue: "End value",
        Step: "Step",
        StepType: "Step type",
        IncludeLastValue: "Include last value"
      },
      StartLoopForeach: {
        Name: "Name",
        State: "State",
        Heading: "Loop",
        ValuesError: "None value cannot be empty!",
        Values: "Values",
        LoopName: "Loop name",
        LoopStateParameterName: "Loop state parameter name",
        LoopCounterValueParameterName: "Loop counter value parameter name",
        Reverse: "Reverse"
      },
      CreateProcess: {
        Name: "Name",
        State: "State",
        Heading: "Main settings",
        ParameterName: "Name",
        ParameterPurpose: "Purpose",
        ParameterValue: "Value",
        Scheme: "Scheme",
        ProcessId: "Process id",
        ProcessCreationParameters: "Process creation parameters",
        ParameterInitialValue: "Initial value"
      },
      AssignmentCreate: {
        Name: "Name",
        State: "State",
        Heading: "Assignment",
        Code: "Code",
        NewTag: "New Tag",
        Executors: "Executors",
        IsActive: "Is active",
        Description: "Description",
        DeadlineToStart: "Deadline to start",
        DeadlineToComplete: "Deadline to complete",
        Observers: "Observers",
        Tags: "Tags"
      },
      AssignmentChange: {
        Name: "Name",
        State: "State",
        Heading: "Assignment",
        Code: "Code",
        ReplacementExecutors: "Replacement of executors",
        OldExecutor: "Old executor",
        NewExecutor: "New executor",
        AllExecutors: "All",
        Change: "Change",
        Status: "Status",
        DeadlineToStart: "Deadline to start",
        DeadlineToComplete: "Deadline to complete",
        Tags: "Tags",
        Observers: "Observers",
        NewTag: "New Tag",
        StatusState: "Status state",
        IsActive: "Is active"
      },
      AssignmentsHaveStatus: {
        Name: "Name",
        State: "State",
        Code: "Code",
        Condition: "Condition",
        IncludeInactive: "Include inactive",
        StatusState: "Status state",
        Assignments: "Assignments"
      },
      SetLoopState: {
        Name: "Name",
        State: "State",
        LoopName: "Loop name",
        LoopState: "Loop state"
      }
    },
    IncorrectForm: "Your form contains greater than one element, and at least one element doesn't have a Name. Please specify the Name for each element."
  },
  ToolbarLabel: {
    CreateActivity: "Créer une activité",
    CreateInline: "Create inline (template)",
    CopySelected: "Copier sélectionné",
    Undo: "annuler",
    Redo: "Refaire",
    Move: "Bouge toi",
    ZoomIn: "Agrandir",
    ZoomOut: "Dézoomer",
    FitToScreen: "Fit to screen",
    ZoomTo100: "Set zoom to 100%",
    FullScreen: "Plein écran",
    Refresh: "Rafraîchir",
    AutoArrangement: "Arrangement automatique",
    Actors: "Acteurs",
    Commands: "Les commandes",
    Parameters: "Paramètres",
    Assignments: "Assignments",
    Localization: "Localisation",
    Timers: "Minuteries",
    AdditionalParameters: "Paramètres additionnels",
    CodeActions: "Actions de code",
    Info: "Informations étendues",
    Delete: "Effacer",
    Clone: "Cloner",
    Settings: "Réglages",
    CreateTransition: "Créer une transition",
    CreateActivityTransition: "Créer une activité et une transition",
    Legend: "Légende",
    ProcessInfo: "Process Info",
    Inline: "The scheme can be inlined",
    Elements: "Elements",
    Library: "Library",
    ResetSettings: "Reset settings",
    ZoomPositionDefault: "Zoom par défaut"
  },
  LibraryLabel: {PluginRequired: "Plugin required:"},
  Logs: {
    Label: "Logs",
    ParamName: "LogEnabled",
    Description: "This parameter is the default value for logging processes based on this scheme",
    Refresh: "Refresh",
    AutoRefresh: "Auto refresh",
    Empty: "Empty",
    Timestamp: "Timestamp",
    Message: "Message",
    Exception: "Exception",
    Show: "Show",
    ExceptionInfo: "Exception info",
    HideDetails: "Hide details",
    ShowDetails: "Show details"
  },
  ErrorActivityIsInitialCountText: "Un élément doit être marqué flag Initial",
  ErrorActivityIsFinalCountText: "This scheme is Inlined. One or more elements must be marked flag Final",
  ErrorReadOnlySaveText: "Le concepteur en mode lecture seule, vous ne pouvez pas l'enregistrer.",
  ErrorInvalidObjectsSaveText: "Can't save the schema. Those objects are in invalid state:",
  BrokenReferencesDialogText: "This schema contains references to ELEMENTS that aren't defined by this schema or current action providers. Do you want to keep them?",
  EditCodeSettings: {
    Height: 600,
    Width: 1000,
    CodeHeight: 390,
    MessageBoxHeight: 400,
    MessageBoxWidth: 600,
    SuccessBoxHeight: 150,
    SuccessBoxWidth: 300
  },
  EditCodeLabel: {
    Title: "Éditer le code",
    EditCodeButton: "Éditer le code",
    Usings: "Usings",
    Compile: "Compiler",
    CompileSucceeded: "La compilation a réussi.",
    Success: "Succès",
    Error: "Erreur",
    OK: "D'accord",
    ShowUsings: "Afficher les usings",
    HideUsings: "Masquer les usings"
  },
  EditJSONLabel: {
    Title: "Modifier la valeur en JSON",
    CreateEmptyType: "Créer",
    Format: "Format"
  },
  EditExpressionSettings: {
    Height: 600,
    Width: 1000,
    CodeHeight: 390,
    MessageBoxHeight: 400,
    MessageBoxWidth: 600,
    SuccessBoxHeight: 150,
    SuccessBoxWidth: 300
  },
  EditExpressionLabel: {
    Title: "Edit expression",
    Validate: "Validate",
    Succeeded: "Expression is valid.",
    Success: "Success",
    Error: "Error",
    OK: "OK"
  },
  Elements: {
    Header: "Elements",
    BasicActivity: "Basic Activity",
    BasicActivityDesc: "It determines a state and the order, in which Actions are executed in your process.",
    InlineHeader: "Inline",
    InitialActivity: "Initial Activity",
    InitialActivityDesc: "It a initial state.",
    FinalActivity: "Final Activity",
    FinalActivityDesc: "It a final state.",
    SetLoopState: {
      Title: "Set loop state",
      Description: "The action for set loop state"
    }
  },
  OverviewMap: {show: true, width: 300, height: 150},
  UndoDepth: 200,
  DefaultCulture: "fr-FR",
  ErrorInBrowserConsole: "See more info in the browser console.",
  ELEMENT: {
    el: {
      colorpicker: {confirm: "OK", clear: "Effacer"},
      datepicker: {
        now: "Maintenant",
        today: "Auj.",
        cancel: "Annuler",
        clear: "Effacer",
        confirm: "OK",
        selectDate: "Choisir date",
        selectTime: "Choisir horaire",
        startDate: "Date début",
        startTime: "Horaire début",
        endDate: "Date fin",
        endTime: "Horaire fin",
        prevYear: "Année précédente",
        nextYear: "Année suivante",
        prevMonth: "Mois précédent",
        nextMonth: "Mois suivant",
        year: "",
        month1: "Janvier",
        month2: "Février",
        month3: "Mars",
        month4: "Avril",
        month5: "Mai",
        month6: "Juin",
        month7: "Juillet",
        month8: "Août",
        month9: "Septembre",
        month10: "Octobre",
        month11: "Novembre",
        month12: "Décembre",
        week: "week",
        weeks: {
          sun: "Dim",
          mon: "Lun",
          tue: "Mar",
          wed: "Mer",
          thu: "Jeu",
          fri: "Ven",
          sat: "Sam"
        },
        months: {
          jan: "Jan",
          feb: "Fév",
          mar: "Mar",
          apr: "Avr",
          may: "Mai",
          jun: "Jun",
          jul: "Jul",
          aug: "Aoû",
          sep: "Sep",
          oct: "Oct",
          nov: "Nov",
          dec: "Déc"
        }
      },
      select: {
        loading: "Chargement",
        noMatch: "Aucune correspondance",
        noData: "Aucune donnée",
        placeholder: "Choisir"
      },
      cascader: {
        noMatch: "Aucune correspondance",
        loading: "Chargement",
        placeholder: "Choisir",
        noData: "Aucune donnée"
      },
      pagination: {
        goto: "Aller à",
        pagesize: "/page",
        total: "Total {total}",
        pageClassifier: ""
      },
      messagebox: {
        title: "Message",
        confirm: "Confirmer",
        cancel: "Annuler",
        error: "Erreur"
      },
      upload: {
        deleteTip: "Cliquer sur supprimer pour retirer le fichier",
        delete: "Supprimer",
        preview: "Aperçu",
        continue: "Continuer"
      },
      table: {
        emptyText: "Aucune donnée",
        confirmFilter: "Confirmer",
        resetFilter: "Réinitialiser",
        clearFilter: "Tous",
        sumText: "Somme"
      },
      tree: {emptyText: "Aucune donnée"},
      transfer: {
        noMatch: "Aucune correspondance",
        noData: "Aucune donnée",
        titles: ["Liste 1", "Liste 2"],
        filterPlaceholder: "Entrer un mot clef",
        noCheckedFormat: "{total} elements",
        hasCheckedFormat: "{checked}/{total} coché(s)"
      },
      image: {error: "FAILED"},
      pageHeader: {title: "Back"},
      popconfirm: {confirmButtonText: "Yes", cancelButtonText: "No"},
      empty: {description: "Aucune donnée"}
    }
  },
  EditJSONSettings: {Height: 600, Width: 1000, CodeHeight: 480}
};

window.WorkflowDesignerConstants = WorkflowDesignerConstants;