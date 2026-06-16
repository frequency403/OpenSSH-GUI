; ============================================================
;  OpenSSH-GUI — NSIS Installer Script
;  Placeholders (@@VAR@@) are substituted at build time
;  via sed in the create_installer job of deploy-release.yml
; ============================================================

!define APP_NAME    "OpenSSH GUI"
!define APP_VERSION "@@VERSION@@"
!define PUBLISHER   "frequency403"
!define EXE_NAME    "OpenSSH-GUI.exe"
!define INSTALL_DIR "$PROGRAMFILES64\OpenSSH-GUI"
!define REG_KEY     "Software\Microsoft\Windows\CurrentVersion\Uninstall\OpenSSH-GUI"
!define SOURCE_EXE  "@@SOURCE_EXE@@"

; ── Output ────────────────────────────────────────────────
Name             "${APP_NAME} ${APP_VERSION}"
OutFile          "OpenSSH-GUI-${APP_VERSION}-Setup.exe"
InstallDir       "${INSTALL_DIR}"
InstallDirRegKey HKLM "Software\OpenSSH-GUI" "Install_Dir"
RequestExecutionLevel admin
SetCompressor /SOLID lzma

; ── Icons ─────────────────────────────────────────────────
Icon    "installer-icon.ico"

; ── MUI2 ──────────────────────────────────────────────────
!include "MUI2.nsh"

!define MUI_ABORTWARNING
!define MUI_ICON   "installer-icon.ico"
!define MUI_UNICON "installer-icon.ico"

!define MUI_WELCOMEPAGE_TITLE     "Installing ${APP_NAME} ${APP_VERSION}"
!define MUI_WELCOMEPAGE_TEXT      "This wizard will install ${APP_NAME} on your computer.$\r$\n$\r$\nClick Next to continue."
!define MUI_FINISHPAGE_RUN        "$INSTDIR\${EXE_NAME}"
!define MUI_FINISHPAGE_RUN_TEXT   "Launch ${APP_NAME}"

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "English"

; ── Install ───────────────────────────────────────────────
Section "Install" SEC_MAIN
  SetOutPath "$INSTDIR"

  ; Artifact rename: build-time name → final EXE name
  File "/oname=${EXE_NAME}" "${SOURCE_EXE}"

  ; Start Menu
  CreateDirectory "$SMPROGRAMS\${APP_NAME}"
  CreateShortcut  "$SMPROGRAMS\${APP_NAME}\${APP_NAME}.lnk" \
    "$INSTDIR\${EXE_NAME}" "" "$INSTDIR\${EXE_NAME}" 0
  CreateShortcut  "$SMPROGRAMS\${APP_NAME}\Uninstall.lnk" \
    "$INSTDIR\uninstall.exe"

  WriteUninstaller "$INSTDIR\uninstall.exe"

  ; Add/Remove Programs
  WriteRegStr   HKLM "${REG_KEY}" "DisplayName"          "${APP_NAME}"
  WriteRegStr   HKLM "${REG_KEY}" "DisplayVersion"       "${APP_VERSION}"
  WriteRegStr   HKLM "${REG_KEY}" "Publisher"            "${PUBLISHER}"
  WriteRegStr   HKLM "${REG_KEY}" "InstallLocation"      "$INSTDIR"
  WriteRegStr   HKLM "${REG_KEY}" "UninstallString"      '"$INSTDIR\uninstall.exe" /S'
  WriteRegStr   HKLM "${REG_KEY}" "QuietUninstallString" '"$INSTDIR\uninstall.exe" /S'
  WriteRegDWORD HKLM "${REG_KEY}" "NoModify"             1
  WriteRegDWORD HKLM "${REG_KEY}" "NoRepair"             1
SectionEnd

; ── Uninstall ─────────────────────────────────────────────
Section "Uninstall"
  Delete "$INSTDIR\${EXE_NAME}"
  Delete "$INSTDIR\uninstall.exe"
  RMDir  "$INSTDIR"

  Delete "$SMPROGRAMS\${APP_NAME}\${APP_NAME}.lnk"
  Delete "$SMPROGRAMS\${APP_NAME}\Uninstall.lnk"
  RMDir  "$SMPROGRAMS\${APP_NAME}"

  DeleteRegKey HKLM "${REG_KEY}"
  DeleteRegKey HKLM "Software\OpenSSH-GUI"
SectionEnd
