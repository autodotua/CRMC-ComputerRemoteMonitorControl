namespace CRMC.Common
{
    public enum ApiCommand
    {
        //A：控制端
        //B：被控端
        //C：客户端
        //S：服务端

        #region 基础通信
        C_Login,
        S_LoginFeedback,
        C_Exit,
        S_ClientsUpdate,
        C_AskForClientList,
        S_NoSuchClient,
        #endregion

        #region 屏幕监控
        Screen_AskForStartScreen,
        Screen_NewScreen,
        Screen_AskForNextScreen,
        Screen_AskForStopScreen,
        #endregion

        #region 系统硬件信息
        WMI_AskForNamespaces,
        WMI_Namespace,

        WMI_AskForClasses,
        WMI_Classes,

        WMI_AskForProps,
        WMI_Props,

        #endregion

        #region 文件系统

        File_AskForRootDirectory,
        File_RootDirectory,
        File_AskForDirectoryContent,
        File_DirectoryContent,

        File_AskForDownloading,
        File_Download,
        File_CanSendDownloadPartAgain,
        File_ReadDownloadFileError,
        File_AskForCancelDownload,


        #endregion

    }

    public enum ControlType
    {
        Screen,
        WMI
    }
}
