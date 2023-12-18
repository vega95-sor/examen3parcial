using Firebase.Auth.Providers;
using Firebase.Auth;
using Firebase_modelo_singleton.Models;
using Microsoft.Maui.Controls;
using Plugin.Maui.Audio;
using System;
using Firebase.Storage;
using Plugin.Media;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;

namespace Firebase_modelo_singleton
{
    public partial class Page_update : ContentPage
    {
        private Notas personaSeleccionada;
        string authdomain = "examen3-6b4c5.firebaseapp.com";
        string apikey = "AIzaSyDALVHE7IaqdY9xuDAm7nP88fiuroQDTCM";
        string email = "ramonmatute2003@gmail.com";
        string password = "Matute_10";
        string token = string.Empty;
        string rutastorage = "examen3-6b4c5.appspot.com";
        string lblaudio;
        string lbl_photo;
        private readonly IAudioRecorder _audioRecorder;
        private bool isRecording = false;
        public string pathaudio, filename, path_photo;
        Plugin.Media.Abstractions.MediaFile photo_camera = null;

        public Page_update()
        {
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

        public void SetPersonaSeleccionada(Notas persona)
        {
            personaSeleccionada = persona;

            descripcionEntry.Text = persona.descripcion;
            photo.Source= persona.photo_record;
            lbl_photo=persona.photo_record;
            lblaudio=persona.audio_record;
            datePicker.Date=new DateTime(persona.fecha.Year,persona.fecha.Month,persona.fecha.Day);
            timePicker.Time=new TimeSpan(persona.fecha.Hour,persona.fecha.Minute,persona.fecha.Second);
        }

        private async void actualizarButton_Clicked(object sender, EventArgs e)
        {
            if (personaSeleccionada != null)
            {
                string nombreAntiguo = personaSeleccionada.descripcion;

                DateTime newDate = new DateTime(datePicker.Date.Year,datePicker.Date.Month,datePicker.Date.Day,timePicker.Time.Hours,timePicker.Time.Minutes,0);
                personaSeleccionada.descripcion = descripcionEntry.Text;
                personaSeleccionada.fecha=newDate;
                personaSeleccionada.audio_record=lblaudio;
                personaSeleccionada.photo_record=lbl_photo;

                DateTime date=new DateTime(DateTime.Now.Year,DateTime.Now.Month,DateTime.Now.Day,DateTime.Now.Hour,DateTime.Now.Minute,0);

                if(date>newDate) {
                    await DisplayAlert("Error","La fecha ya paso, pon una fecha correcta","OK");
                    return;
                }

                if(lblaudio==null) {
                    await DisplayAlert("Error","Graba un audio","OK");
                    return;
                }

                if(lbl_photo==null) {
                    await DisplayAlert("Error","Toma una foto","OK");
                    return;
                }

                try
                {
                    var firebaseInstance = Singleton.Instance;
                    await firebaseInstance.UpdateData(personaSeleccionada.id_nota.ToString(),personaSeleccionada);
                    await DisplayAlert("Éxito", "Datos actualizados correctamente.", "OK");
                    await Navigation.PopAsync();
                }
                catch (Exception ex)
                {
                    personaSeleccionada.descripcion = nombreAntiguo;

                    await DisplayAlert("Error", $"Error al actualizar datos: {ex.Message}", "OK");
                }
            }
            else
            {
                await DisplayAlert("Error", "No se ha seleccionado ninguna persona para actualizar.", "OK");
            }
        }

        private async void listarButton_Clicked(object sender, EventArgs e){
            await Navigation.PushAsync(new Page_list());
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
