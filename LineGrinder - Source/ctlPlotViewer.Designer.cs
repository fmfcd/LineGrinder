namespace LineGrinder
{
    partial class ctlPlotViewer
    {
        /// <summary> 
        /// Variable nécessaire au concepteur.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Nettoyage des ressources utilisées.
        /// </summary>
        /// <param name="disposing">true si les ressources managées doivent être supprimées ; sinon, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Code généré par le Concepteur de composants

        /// <summary> 
        /// Méthode requise pour la prise en charge du concepteur - ne modifiez pas 
        /// le contenu de cette méthode avec l'éditeur de code.
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // ctlPlotViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Name = "ctlPlotViewer";
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.ctlPlotViewer_MouseDown);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.ctlPlotViewer_MouseMove);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.ctlPlotViewer_MouseUp);
            this.ResumeLayout(false);

        }

        #endregion
    }
}
