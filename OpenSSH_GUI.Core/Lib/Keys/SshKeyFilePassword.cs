using System.Buffers;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using ReactiveUI;

namespace OpenSSH_GUI.Core.Lib.Keys;

public sealed record SshKeyFilePassword : INotifyPropertyChanged, IDisposable
{
    private readonly ArrayBufferWriter<byte> _bufferWriter = new();
    private Encoding _encoding = Encoding.UTF8;
    
    public SshKeyFilePassword(ReadOnlyMemory<byte>? password, Encoding? encoding = null)
    {
        if (encoding is not null)
            _encoding = encoding;
        if(password is { } pass)
            WriteToBuffer(pass.Span);
    }   

    public SshKeyFilePassword(string? password, Encoding? encoding = null)
    {
        if (string.IsNullOrWhiteSpace(password))
            return;
        if (encoding is not null)
            _encoding = encoding;
        WriteToBuffer(_encoding.GetBytes(password));
    }

    private void WriteToBuffer(ReadOnlySpan<byte> data)
    {
        _bufferWriter.Write(data);
        OnPropertyChanged(nameof(IsValidPassword));
        OnPropertyChanged(nameof(Password));       
        OnPropertyChanged(nameof(PasswordMemory));       
    }
    
    public void Set(string password, Encoding? encoding = null) => 
        Set((encoding ?? _encoding).GetBytes(password), (encoding ?? _encoding));

    public void Set(ReadOnlyMemory<byte> password, Encoding? encoding = null)
    {
        _bufferWriter.Clear();
        _encoding = encoding ?? _encoding;
        WriteToBuffer(password.Span);
    }
    
    [MemberNotNullWhen(true, nameof(Password), nameof(PasswordMemory))]
    public bool IsValidPassword => _bufferWriter.WrittenSpan is { Length: > 0 };
    
    
    public string? Password => IsValidPassword ? _encoding.GetString(_bufferWriter.WrittenSpan) : null;
    public ReadOnlyMemory<byte>? PasswordMemory => IsValidPassword ? _bufferWriter.WrittenMemory : null;
    
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void Dispose()
    {
        _bufferWriter.ResetWrittenCount();
        _bufferWriter.Clear();
    }
}