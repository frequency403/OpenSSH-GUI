using System.Diagnostics.CodeAnalysis;
using OpenSSH_GUI.Core.Enums;

namespace OpenSSH_GUI.Core.Lib.Misc;

public record KeyManagerOperationResult
{
    [MemberNotNullWhen(false, nameof(Exception))]
    public virtual bool IsSuccess => Result == OperationResult.Success;

    [MemberNotNullWhen(true, nameof(Exception))]
    public bool IsConflict => Result == OperationResult.Conflict;

    [MemberNotNullWhen(true, nameof(Exception))]
    public bool IsCancelled => Result == OperationResult.Cancelled;

    [MemberNotNullWhen(true, nameof(Exception))]
    public bool IsFailure => Result == OperationResult.Failure;

    public OperationResult Result { get; protected init; }
    public Exception? Exception { get; protected init; }

    public static KeyManagerOperationResult Success() => new()
        { Result = OperationResult.Success };

    public static KeyManagerOperationResult<T> Success<T>(T value) => KeyManagerOperationResult<T>.Success(value);

    public static KeyManagerOperationResult FromException(Exception exception) => exception is OperationCanceledException ? Cancelled(exception) : Failure(exception);

    public static KeyManagerOperationResult Failure(Exception exception) => new()
        { Result = OperationResult.Failure, Exception = exception };

    public static KeyManagerOperationResult Conflict(Exception exception) => new()
        { Result = OperationResult.Conflict, Exception = exception };

    internal static KeyManagerOperationResult Cancelled(Exception exception) => new()
        { Result = OperationResult.Cancelled, Exception = exception };

    /// <summary>Throws the associated exception if the result represents a failure.</summary>
    /// <param name="throwOnCancelled">Whether to also throw if the result was cancelled.</param>
    public void ThrowIfFailure(bool throwOnCancelled = true)
    {
        if (IsFailure || throwOnCancelled && IsCancelled)
            throw Exception;
    }

    public KeyManagerOperationResult<T> WithValue<T>(T value) => KeyManagerOperationResult<T>.SetValue(value, this);
}

public sealed record KeyManagerOperationResult<T> : KeyManagerOperationResult
{
#pragma warning disable CS8776
    [MemberNotNullWhen(true, nameof(ResultValue)), MemberNotNullWhen(false, nameof(Exception))]
    public override bool IsSuccess => Result == OperationResult.Success && ResultValue is not null;
#pragma warning restore CS8776

    public T? ResultValue { get; private init; }

    internal static KeyManagerOperationResult<T> SetValue(T value, KeyManagerOperationResult operationResult) => new()
    {
        Exception = operationResult.Exception,
        Result = operationResult.Result,
        ResultValue = value
    };

    public static KeyManagerOperationResult<T> Success(T value) => new()
        { Result = OperationResult.Success, ResultValue = value };
}