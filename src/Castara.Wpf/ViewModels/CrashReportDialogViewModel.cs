using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Castara.Wpf.Diagnostics.CrashReport;

/// <summary>
/// View model for the crash report dialog that allows users to review crash details
/// and choose whether to save locally and/or send to the diagnostic server.
/// </summary>
/// <remarks>
/// <para>
/// This view model manages the UI state and user interactions for the crash report dialog window.
/// It uses the CommunityToolkit.Mvvm source generators to implement INotifyPropertyChanged
/// and command patterns with minimal boilerplate code.
/// </para>
/// 
/// <para>
/// <b>User choices:</b>
/// </para>
/// <list type="bullet">
/// <item><description><see cref="SendReport"/>: Whether to upload crash report to diagnostic server (default: false)</description></item>
/// <item><description><see cref="SaveLocally"/>: Whether to save crash report to local disk (default: true)</description></item>
/// </list>
/// 
/// <para>
/// <b>Immutable crash report data:</b>
/// </para>
/// <list type="bullet">
/// <item><description><see cref="ReportId"/>: Unique identifier displayed to user for reference</description></item>
/// <item><description><see cref="ReportJson"/>: Pretty-printed JSON crash report for user review</description></item>
/// </list>
/// 
/// <para>
/// <b>Commands (user actions):</b>
/// </para>
/// <list type="bullet">
/// <item><description><see cref="CloseCommand"/>: User dismisses dialog without taking action</description></item>
/// <item><description><see cref="ContinueCommand"/>: User confirms choices and proceeds</description></item>
/// <item><description><see cref="CopyCommand"/>: User copies crash report JSON to clipboard</description></item>
/// </list>
/// 
/// <para>
/// <b>Events (communication with dialog/service):</b>
/// </para>
/// <list type="bullet">
/// <item><description><see cref="CloseRequested"/>: Fired when user closes or continues, carries acceptance flag</description></item>
/// <item><description><see cref="CopyRequested"/>: Fired when user copies, carries crash report JSON</description></item>
/// </list>
/// 
/// <para>
/// <b>Example XAML data binding:</b>
/// </para>
/// <code><![CDATA[
/// <Window x:Class="Castara.Wpf.Views.CrashReportDialog"
///         xmlns:vm="clr-namespace:Castara.Wpf.Diagnostics.CrashReport"
///         Title="{Binding Title}">
///     <StackPanel>
///         <!-- Message -->
///         <TextBlock Text="{Binding Message}" TextWrapping="Wrap" />
///         
///         <!-- Report ID -->
///         <TextBlock>
///             <Run Text="Report ID: " />
///             <Run Text="{Binding ReportId}" FontWeight="Bold" />
///         </TextBlock>
///         
///         <!-- Crash Report JSON Viewer -->
///         <TextBox Text="{Binding ReportJson}" 
///                  IsReadOnly="True" 
///                  VerticalScrollBarVisibility="Auto" />
///         
///         <!-- User Choices -->
///         <CheckBox Content="Save crash report locally" 
///                   IsChecked="{Binding SaveLocally}" />
///         <CheckBox Content="Send crash report to diagnostic server" 
///                   IsChecked="{Binding SendReport}" />
///         
///         <!-- Actions -->
///         <Button Content="Copy to Clipboard" Command="{Binding CopyCommand}" />
///         <Button Content="Continue" Command="{Binding ContinueCommand}" />
///         <Button Content="Close" Command="{Binding CloseCommand}" />
///     </StackPanel>
/// </Window>
/// ]]></code>
/// 
/// <para>
/// <b>Example event handling in dialog code-behind:</b>
/// </para>
/// <code>
/// public partial class CrashReportDialog : Window
/// {
///     private readonly CrashReportDialogViewModel _viewModel;
///     
///     public CrashReportDialog(string reportJson, string reportId)
///     {
///         InitializeComponent();
///         
///         _viewModel = new CrashReportDialogViewModel(reportJson, reportId);
///         DataContext = _viewModel;
///         
///         // Handle close request
///         _viewModel.CloseRequested += (s, accepted) =>
///         {
///             DialogResult = accepted;
///             Close();
///         };
///         
///         // Handle copy request
///         _viewModel.CopyRequested += (s, json) =>
///         {
///             Clipboard.SetText(json);
///             MessageBox.Show("Crash report copied to clipboard.");
///         };
///     }
/// }
/// </code>
/// 
/// <para>
/// <b>CommunityToolkit.Mvvm source generators:</b>
/// </para>
/// <list type="bullet">
/// <item><description><c>[ObservableProperty]</c> generates public properties and INotifyPropertyChanged implementation</description></item>
/// <item><description>Generated properties: <c>SendReport</c>, <c>SaveLocally</c></description></item>
/// <item><description>Generated change notifications: <c>OnSendReportChanged</c>, <c>OnSaveLocallyChanged</c></description></item>
/// <item><description><c>RelayCommand</c> provides simple command implementation with CanExecute support</description></item>
/// </list>
/// </remarks>
public partial class CrashReportDialogViewModel : ObservableObject
{
    /// <summary>
    /// Gets or sets a value indicating whether the user wants to send the crash report to the diagnostic server.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the user wants to upload the crash report; otherwise, <see langword="false"/>.
    /// Default is <see langword="false"/> (opt-in for privacy).
    /// </value>
    /// <remarks>
    /// This property is data-bound to a CheckBox in the dialog. The user explicitly opts in to sending
    /// crash reports to protect their privacy. The CommunityToolkit.Mvvm source generator automatically
    /// generates the public <c>SendReport</c> property and <c>OnSendReportChanged</c> notification method.
    /// </remarks>
    [ObservableProperty]
    private bool sendReport = false; // Default: opt-in for privacy

    /// <summary>
    /// Gets or sets a value indicating whether the user wants to save the crash report locally.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the user wants to save the crash report to %LocalAppData%\Castara\CrashReports;
    /// otherwise, <see langword="false"/>. Default is <see langword="true"/> (save locally by default).
    /// </value>
    /// <remarks>
    /// This property is data-bound to a CheckBox in the dialog. Saving locally is enabled by default
    /// so crash information is preserved even if the user doesn't send it to the server. The
    /// CommunityToolkit.Mvvm source generator automatically generates the public <c>SaveLocally</c>
    /// property and <c>OnSaveLocallyChanged</c> notification method.
    /// </remarks>
    [ObservableProperty]
    private bool saveLocally = true; // Default: save locally by default

    /// <summary>
    /// Initializes a new instance of the <see cref="CrashReportDialogViewModel"/> class.
    /// </summary>
    /// <param name="reportJson">The crash report in human-readable, pretty-printed JSON format.</param>
    /// <param name="reportId">The unique crash report identifier (e.g., "abc123def456").</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="reportJson"/> or <paramref name="reportId"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// This constructor initializes all commands and stores the immutable crash report data.
    /// The <paramref name="reportJson"/> and <paramref name="reportId"/> are set to read-only
    /// properties and cannot be changed after construction.
    /// </remarks>
    public CrashReportDialogViewModel(string reportJson, string reportId)
    {
        ReportJson = reportJson ?? throw new ArgumentNullException(nameof(reportJson));
        ReportId = reportId ?? throw new ArgumentNullException(nameof(reportId));

        CloseCommand = new RelayCommand(Close);
        ContinueCommand = new RelayCommand(Continue);
        CopyCommand = new RelayCommand(Copy);
    }

    /// <summary>
    /// Gets the unique crash report identifier displayed to the user for reference.
    /// </summary>
    /// <value>
    /// The crash report ID (e.g., "abc123def456"). This value is immutable after construction.
    /// </value>
    /// <remarks>
    /// Users can reference this ID when contacting support or filing bug reports. The ID is
    /// typically displayed prominently in the dialog header or as a separate text field.
    /// </remarks>
    public string ReportId { get; }

    /// <summary>
    /// Gets the crash report in human-readable, pretty-printed JSON format.
    /// </summary>
    /// <value>
    /// The crash report JSON with indentation. This value is immutable after construction.
    /// </value>
    /// <remarks>
    /// This JSON contains all crash report details including exception information, stack traces,
    /// application state, and recent logs. It's displayed in a scrollable, read-only text viewer
    /// so users can review the information before deciding whether to save or send it.
    /// </remarks>
    public string ReportJson { get; }

    /// <summary>
    /// Gets the dialog window title.
    /// </summary>
    /// <value>
    /// The fixed title: "Castara encountered an unexpected error".
    /// </value>
    public string Title => "Castara encountered an unexpected error";

    /// <summary>
    /// Gets the explanatory message displayed to the user.
    /// </summary>
    /// <value>
    /// A user-friendly message explaining what happened and what actions are available.
    /// </value>
    /// <remarks>
    /// This message is displayed at the top of the dialog to provide context and instructions
    /// to the user about what they can do with the crash report.
    /// </remarks>
    public string Message =>
        "Castara ran into an unexpected problem and needs to close. " +
        "You can review the diagnostic report below and choose whether to save or send it for analysis.";

    /// <summary>
    /// Gets the command executed when the user dismisses the dialog without taking action.
    /// </summary>
    /// <value>
    /// A <see cref="IRelayCommand"/> that raises <see cref="CloseRequested"/> with <see langword="false"/>.
    /// </value>
    /// <remarks>
    /// This command is typically bound to a "Close" or "Cancel" button. When executed, it raises
    /// the <see cref="CloseRequested"/> event with <c>accepted = false</c>, indicating the user
    /// dismissed the dialog without confirming their choices.
    /// </remarks>
    public IRelayCommand CloseCommand { get; }

    /// <summary>
    /// Gets the command executed when the user confirms their choices and proceeds.
    /// </summary>
    /// <value>
    /// A <see cref="IRelayCommand"/> that raises <see cref="CloseRequested"/> with <see langword="true"/>.
    /// </value>
    /// <remarks>
    /// This command is typically bound to a "Continue", "OK", or "Submit" button. When executed,
    /// it raises the <see cref="CloseRequested"/> event with <c>accepted = true</c>, and the dialog
    /// should process the user's <see cref="SendReport"/> and <see cref="SaveLocally"/> choices.
    /// </remarks>
    public IRelayCommand ContinueCommand { get; }

    /// <summary>
    /// Gets the command executed when the user copies the crash report to the clipboard.
    /// </summary>
    /// <value>
    /// A <see cref="IRelayCommand"/> that raises <see cref="CopyRequested"/> with <see cref="ReportJson"/>.
    /// </value>
    /// <remarks>
    /// This command is typically bound to a "Copy to Clipboard" button. When executed, it raises
    /// the <see cref="CopyRequested"/> event with the crash report JSON, which the view can copy
    /// to the system clipboard using <c>Clipboard.SetText()</c>.
    /// </remarks>
    public IRelayCommand CopyCommand { get; }

    /// <summary>
    /// Occurs when the user closes or continues the dialog.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The event argument (<see cref="bool"/>) indicates whether the user accepted the dialog:
    /// </para>
    /// <list type="bullet">
    /// <item><description><see langword="true"/>: User clicked "Continue" - process <see cref="SendReport"/> and <see cref="SaveLocally"/></description></item>
    /// <item><description><see langword="false"/>: User clicked "Close" or dismissed - discard user's choices</description></item>
    /// </list>
    /// 
    /// <para>
    /// <b>Example handler in dialog code-behind:</b>
    /// </para>
    /// <code>
    /// _viewModel.CloseRequested += (sender, accepted) =>
    /// {
    ///     DialogResult = accepted;
    ///     Close();
    /// };
    /// </code>
    /// </remarks>
    public event EventHandler<bool>? CloseRequested;

    /// <summary>
    /// Occurs when the user requests to copy the crash report to the clipboard.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The event argument (<see cref="string"/>) contains the crash report JSON from <see cref="ReportJson"/>.
    /// </para>
    /// 
    /// <para>
    /// <b>Example handler in dialog code-behind:</b>
    /// </para>
    /// <code>
    /// _viewModel.CopyRequested += (sender, json) =>
    /// {
    ///     Clipboard.SetText(json);
    ///     MessageBox.Show("Crash report copied to clipboard.");
    /// };
    /// </code>
    /// </remarks>
    public event EventHandler<string>? CopyRequested;

    /// <summary>
    /// Handles the Close command by raising <see cref="CloseRequested"/> with <see langword="false"/>.
    /// </summary>
    /// <remarks>
    /// This method is invoked when <see cref="CloseCommand"/> is executed. The <c>accepted = false</c>
    /// parameter indicates the user dismissed the dialog without confirming their choices.
    /// </remarks>
    private void Close() => CloseRequested?.Invoke(this, false); // User dismissed dialog

    /// <summary>
    /// Handles the Continue command by raising <see cref="CloseRequested"/> with <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// This method is invoked when <see cref="ContinueCommand"/> is executed. The <c>accepted = true</c>
    /// parameter indicates the user confirmed their choices and the dialog should process the
    /// <see cref="SendReport"/> and <see cref="SaveLocally"/> values.
    /// </remarks>
    private void Continue() => CloseRequested?.Invoke(this, true); // User accepted/confirmed

    /// <summary>
    /// Handles the Copy command by raising <see cref="CopyRequested"/> with <see cref="ReportJson"/>.
    /// </summary>
    /// <remarks>
    /// This method is invoked when <see cref="CopyCommand"/> is executed. The view should handle
    /// this event and copy the JSON to the system clipboard using <c>Clipboard.SetText()</c>.
    /// </remarks>
    private void Copy() => CopyRequested?.Invoke(this, ReportJson);
}
