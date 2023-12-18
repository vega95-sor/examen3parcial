namespace Firebase_modelo_singleton {
    public partial class App:Application {
        public App() {
            InitializeComponent();

            MainPage=new AppShell();
        }
    }
}