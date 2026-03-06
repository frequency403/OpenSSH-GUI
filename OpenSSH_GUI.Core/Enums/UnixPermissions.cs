namespace OpenSSH_GUI.Core.Enums;

public enum UnixPermissions
{
    // Owner
    OwnerRead = 400,
    OwnerWrite = 200,
    OwnerExecute = 100,

    // Group
    GroupRead = 40,
    GroupWrite = 20,
    GroupExecute = 10,

    // Others
    OthersRead = 4,
    OthersWrite = 2,
    OthersExecute = 1,

    // Common permissions
    OwnerReadWrite = 600,
    OwnerReadWriteExecute = 700,
    GroupAndOthersReadExecute = 55,
    AllReadWriteExecute = 777,
    AllReadWrite = 666,
    Default = 644
}