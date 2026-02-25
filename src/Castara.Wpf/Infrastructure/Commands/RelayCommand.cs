using System;
using System.Windows.Input;

namespace Castara.Wpf.Infrastructure.Commands;

/// <summary>
/// Provides a reusable implementation of <see cref="ICommand"/> that delegates execution
/// and can-execute logic to injected delegates, following the MVVM pattern.
/// </summary>
/// <remarks>
/// <para>
/// This command implementation allows view models to expose command logic without
/// implementing <see cref="ICommand"/> directly on each command property. The relay
/// pattern simplifies command creation by accepting delegates for both execution
/// and conditional availability.
/// </para>
/// <para>
/// Key features:
/// <list type="bullet">
///   <item><description>Parameterless command execution (suitable for button clicks, menu items)</description></item>
///   <item><description>Optional can-execute predicate for conditional enablement</description></item>
///   <item><description>Manual trigger of CanExecuteChanged via <see cref="RaiseCanExecuteChanged"/></description></item>
/// </list>
/// </para>
/// <para>
/// This implementation is suitable for commands that don't require parameters. For
/// parameterized commands, a generic RelayCommand&lt;T&gt; variant would be needed.
/// </para>
/// </remarks>
/// <example>
/// Example usage in a view model:
/// <code>
/// public class MyViewModel
/// {
///     public ICommand SaveCommand { get; }
///     public ICommand DeleteCommand { get; }
///     
///     private bool _hasChanges;
///     
///     public MyViewModel()
///     {
///         // Simple command without can-execute logic
///         SaveCommand = new RelayCommand(Save);
///         
///         // Command with conditional execution
///         DeleteCommand = new RelayCommand(Delete, CanDelete);
///     }
///     
///     private void Save()
///     {
///         // Save logic here
///         _hasChanges = false;
///         ((RelayCommand)DeleteCommand).RaiseCanExecuteChanged();
///     }
///     
///     private void Delete()
///     {
///         // Delete logic here
///     }
///     
///     private bool CanDelete()
///     {
///         return !_hasChanges;
///     }
/// }
/// </code>
/// </example>
public sealed class RelayCommand : ICommand
{
    /// <summary>
    /// The action to execute when the command is invoked.
    /// </summary>
    private readonly Action _execute;

    /// <summary>
    /// The optional function to determine whether the command can execute.
    /// </summary>
    private readonly Func<bool>? _canExecute;

    /// <summary>
    /// Initializes a new instance of the <see cref="RelayCommand"/> class.
    /// </summary>
    /// <param name="execute">
    /// The action to execute when the command is invoked. This parameter is required.
    /// </param>
    /// <param name="canExecute">
    /// An optional function that determines whether the command can execute.
    /// If null, the command is always available for execution.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="execute"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The <paramref name="execute"/> delegate is invoked when the command is triggered
    /// by user interaction (e.g., button click) or programmatic execution.
    /// </para>
    /// <para>
    /// The <paramref name="canExecute"/> delegate, if provided, is evaluated to determine
    /// whether the command should be enabled in the UI. When this returns false, bound
    /// controls (buttons, menu items) are automatically disabled.
    /// </para>
    /// <para>
    /// To update the enabled state after property changes, call <see cref="RaiseCanExecuteChanged"/>
    /// to notify the UI that the can-execute condition may have changed.
    /// </para>
    /// </remarks>
    public RelayCommand(
        Action execute,
        Func<bool>? canExecute = null)
    {
        _execute = execute
            ?? throw new ArgumentNullException(nameof(execute));

        _canExecute = canExecute;
    }

    /// <summary>
    /// Determines whether the command can execute in its current state.
    /// </summary>
    /// <param name="parameter">
    /// Data used by the command. This parameter is ignored in this implementation
    /// as the command is parameterless.
    /// </param>
    /// <returns>
    /// <c>true</c> if the command can execute; otherwise, <c>false</c>.
    /// If no can-execute predicate was provided in the constructor, always returns <c>true</c>.
    /// </returns>
    /// <remarks>
    /// WPF calls this method to determine whether bound controls should be enabled or disabled.
    /// The result is cached by WPF until <see cref="CanExecuteChanged"/> is raised.
    /// </remarks>
    public bool CanExecute(object? parameter)
        => _canExecute?.Invoke() ?? true;

    /// <summary>
    /// Executes the command logic.
    /// </summary>
    /// <param name="parameter">
    /// Data used by the command. This parameter is ignored in this implementation
    /// as the command is parameterless.
    /// </param>
    /// <remarks>
    /// <para>
    /// This method invokes the execute delegate provided in the constructor.
    /// WPF automatically calls this method when the command is triggered through
    /// bound UI elements (buttons, menu items, key bindings, etc.).
    /// </para>
    /// <para>
    /// The method can also be called programmatically if needed, though this is
    /// less common in typical MVVM scenarios.
    /// </para>
    /// </remarks>
    public void Execute(object? parameter)
        => _execute();

    /// <summary>
    /// Occurs when changes occur that affect whether the command should execute.
    /// </summary>
    /// <remarks>
    /// <para>
    /// WPF subscribes to this event to monitor when command availability changes.
    /// When raised, WPF re-evaluates <see cref="CanExecute"/> and updates the
    /// enabled state of bound controls.
    /// </para>
    /// <para>
    /// Unlike <see cref="CommandManager.RequerySuggested"/>, this event must be
    /// raised manually via <see cref="RaiseCanExecuteChanged"/> when view model
    /// state changes affect command availability.
    /// </para>
    /// </remarks>
    public event EventHandler? CanExecuteChanged;

    /// <summary>
    /// Raises the <see cref="CanExecuteChanged"/> event to notify the UI that the
    /// command's execution state may have changed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Call this method from the view model when properties or conditions change
    /// that affect whether the command should be enabled. For example:
    /// <list type="bullet">
    ///   <item><description>After completing an operation that changes data validity</description></item>
    ///   <item><description>When selection changes affect available actions</description></item>
    ///   <item><description>After property changes that the can-execute predicate depends on</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// This causes WPF to re-evaluate <see cref="CanExecute"/> and update the UI
    /// accordingly, enabling or disabling bound controls.
    /// </para>
    /// </remarks>
    /// <example>
    /// Example of updating command state:
    /// <code>
    /// private bool _hasSelection;
    /// 
    /// public bool HasSelection
    /// {
    ///     get => _hasSelection;
    ///     set
    ///     {
    ///         _hasSelection = value;
    ///         ((RelayCommand)DeleteCommand).RaiseCanExecuteChanged();
    ///     }
    /// }
    /// </code>
    /// </example>
    public void RaiseCanExecuteChanged()
        => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}