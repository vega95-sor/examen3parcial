using Firebase_modelo_singleton.Models;
using Microsoft.Maui.Controls;
using Plugin.Maui.Audio;
using Firebase.Auth;
using Firebase.Auth.Providers;
using System;
using Firebase.Storage;
using Plugin.Media;
using System.Globalization;

namespace Firebase_modelo_singleton{
    public partial class MainPage : ContentPage{
        string authdomain = "examen3-6b4c5.firebaseapp.com";
        string apikey = "AIzaSyDALVHE7IaqdY9xuDAm7nP88fiuroQDTCM";
        string email = "ramonmatute2003@gmail.com";
        string password = "Matute_10";
        string token = string.Empty;
        string rutastorage = "examen3-6b4c5.appspot.com";
        string lblaudio=null;
        string lbl_photo=null;
        private readonly IAudioRecorder _audioRecorder;
        private bool isRecording = false;
        public string pathaudio, filename, path_photo;
        Plugin.Media.Abstractions.MediaFile photo_camera = null;

        public MainPage(){
            InitializeComponent();

            _audioRecorder=AudioManager.Current.CreateRecorder();

            MainThread.BeginInvokeOnMainThread(new Action(async () => await obtenerToken()));
        }

        private async Task obtenerToken() {
            var client = new FirebaseAuthClient(new FirebaseAuthConfig() {
                ApiKey=apikey,
                AuthDomain=authdomain,
                Providers=new FirebaseAuthProvider[]
                {
                    new EmailProvider()
                }
            });

            var credenciales = await client.SignInWithEmailAndPasswordAsync(email,password);
            token=await credenciales.User.GetIdTokenAsync();
        }

        private async void guardarButton_Clicked(object sender, EventArgs e){
            string description = descripcionEntry.Text;

            if (string.IsNullOrWhiteSpace(description)){
                await DisplayAlert("Error", "Por favor, completa todos los campos.", "OK");
                return;
            }

            DateTime newDate = new DateTime(datePicker.Date.Year,datePicker.Date.Month,datePicker.Date.Day,timePicker.Time.Hours,timePicker.Time.Minutes,0);

            DateTime date = new DateTime(DateTime.Now.Year,DateTime.Now.Month,DateTime.Now.Day,DateTime.Now.Hour,DateTime.Now.Minute,0);

            if(date>newDate) {
                await DisplayAlert("Error","La fecha ya paso, pon una fecha correcta","OK");
                return;
            }

            if(lblaudio==null){
                await DisplayAlert("Error","Graba un audio","OK");
                return;
            }

            if(lbl_photo==null){
                await DisplayAlert("Error","Toma una foto","OK");
                return;
            }

            try {
                var firebaseInstance = Singleton.Instance;
                Notas persona = new Notas { descripcion =description, fecha=newDate, audio_record=lblaudio, photo_record=lbl_photo};

                await firebaseInstance.CreateData(persona);

                await DisplayAlert("Éxito", "Datos subidos correctamente.", "OK");

                descripcionEntry.Text = string.Empty;
                lbl_photo=null;
                lblaudio=null;
                photo.Source=null;
            }catch (Exception ex){
                await DisplayAlert("Error", $"Error al subir datos: {ex.Message}", "OK");
            }
        }

        private async void listarButton_Clicked(object sender, EventArgs e){
            await Navigation.PushAsync(new Page_list());
        }

        private async Task<bool> CheckAndRequestStoragePermissionAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.StorageWrite>();
        
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.StorageWrite>();
            
            if (status != PermissionStatus.Granted)
            {
                // Permiso denegado
                return false;
            }
        }

        // Permiso otorgado
        return true;
    }

        private async void btnGrabarAudio_Clicked(object sender,EventArgs e) {

            if(!isRecording) {
                var permiso = await Permissions.RequestAsync<Permissions.Microphone>();
                var permiso1 = await Permissions.RequestAsync<Permissions.StorageRead>();
                var permiso2 = await Permissions.RequestAsync<Permissions.StorageWrite>();

                if(permiso!=PermissionStatus.Granted||permiso1!=PermissionStatus.Granted||permiso2!=PermissionStatus.Granted) {
                    return;
                }

                await _audioRecorder.StartAsync();
                isRecording=true;
                btnGrabarAudio.Text="Grabando";
                Console.WriteLine("Iniciando grabación...");
            } else {
                var recordedAudio = await _audioRecorder.StopAsync();

                if(recordedAudio!=null) {
                    try {
                        filename=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),DateTime.Now.ToString("ddMMyyyymmss")+"_VoiceNote.wav");

                        using(var fileStorage = new FileStream(filename,FileMode.Create,FileAccess.Write)) {
                            recordedAudio.GetAudioStream().CopyTo(fileStorage);
                        }

                        pathaudio=filename;

                        var task = new FirebaseStorage(
                            rutastorage,
                            new FirebaseStorageOptions {
                                AuthTokenAsyncFactory=() => Task.FromResult(token),
                                ThrowOnCancel=true
                            }
                        )
                        .Child("Audios")
                        .Child(Path.GetFileName(pathaudio))
                        .PutAsync(File.OpenRead(pathaudio));

                        var urlDescarga = await task;
                        lblaudio=urlDescarga;
                        lblUrl.Text=urlDescarga;
                    } catch(Exception ex) {
                        Console.WriteLine($"Error: {ex.Message}");
                        await DisplayAlert("Error","Ocurrió un error al procesar la grabación.","Ok");
                    }
                } else {
                    await DisplayAlert("Error","La grabación de audio ha fallado.","Ok");
                }
                isRecording=false;
                btnGrabarAudio.Text="Grabar Audio";
                Console.WriteLine("Deteniendo grabación y guardando el audio...");
            }
        }

        private async void btn_photo_Clicked(object sender,EventArgs e) {
            photo_camera=await CrossMedia.Current.TakePhotoAsync(new Plugin.Media.Abstractions.StoreCameraMediaOptions {
                Directory="MiAlbum",
                Name="Foto.jpg",
                SaveToAlbum=true
            });

            if(photo_camera!=null) {
                try {
                    filename=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),DateTime.Now.ToString("ddMMyyyymmss")+"_img.jpg");

                    using(var fileStorage = new FileStream(filename,FileMode.Create,FileAccess.Write)) {
                        photo_camera.GetStream().CopyTo(fileStorage);
                    }

                    path_photo=filename;

                    var task = new FirebaseStorage(
                        rutastorage,
                        new FirebaseStorageOptions {
                            AuthTokenAsyncFactory=() => Task.FromResult(token),
                            ThrowOnCancel=true
                        }
                    )
                    .Child("Fotos")
                    .Child(Path.GetFileName(path_photo))
                    .PutAsync(File.OpenRead(path_photo));

                    var urlDescarga = await task;
                    lbl_photo=urlDescarga;
                } catch(Exception ex) {
                    Console.WriteLine($"Error: {ex.Message}");
                    await DisplayAlert("Error","Ocurrió un error al procesar la grabación.","Ok");
                }

                photo.Source=ImageSource.FromStream(() => {
                    return photo_camera.GetStream();
                });
            }
        }
    }
}
