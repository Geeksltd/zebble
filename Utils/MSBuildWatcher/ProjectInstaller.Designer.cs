namespace MSBuildWatcher
{
    partial class ProjectInstaller
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        void InitializeComponent()
        {
            serviceProcessInstaller1 = new System.ServiceProcess.ServiceProcessInstaller();
            serviceInstaller1 = new System.ServiceProcess.ServiceInstaller();
            //
            // serviceProcessInstaller1
            //
            serviceProcessInstaller1.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            serviceProcessInstaller1.Password = null;
            serviceProcessInstaller1.Username = null;
            //
            // serviceInstaller1
            //
            serviceInstaller1.ServiceName = "MSbuild (idle) Killer";
            serviceInstaller1.Description = "Watches msbuild.exe processes and if they take longer than 3 mins, it kills them!";
            //
            // ProjectInstaller
            //
            Installers.AddRange(new System.Configuration.Install.Installer[] {
            serviceProcessInstaller1,
            serviceInstaller1});
        }

        #endregion

        System.ServiceProcess.ServiceProcessInstaller serviceProcessInstaller1;
        System.ServiceProcess.ServiceInstaller serviceInstaller1;
    }
}