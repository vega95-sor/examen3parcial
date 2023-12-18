using CommunityToolkit.Maui.Views;
using Firebase.Database;
using Firebase_modelo_singleton.Models;
using Microsoft.Maui.Controls;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.ConstrainedExecution;

namespace Firebase_modelo_singleton
{
    public partial class Page_list : ContentPage
    {
        public static string id_nota;
        public static string audio_record, photo_record, descripcion;
        public static string fecha;

        public ObservableCollection<Notas> PeopleList { get; set; }
        
        public Page_list()
        {
            InitializeComponent();

            PeopleList = new ObservableCollection<Notas>();
            LoadData(); 
        }

        protected override async void OnAppearing() {
            base.OnAppearing();

            await LoadData();
        }

        public void SetPeopleList(ObservableCollection<Notas> people)
        {
            PeopleList.Clear();

            foreach (var person in people)
            {
                PeopleList.Add(person);
            }

            var sortedList = new List<Notas>(PeopleList);
            sortedList=sortedList.OrderBy(i => i.fecha).ToList();

            PeopleList=new ObservableCollection<Notas>(sortedList);

            peopleListView.ItemsSource=PeopleList;
        }

        private async void peopleListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem == null)
                return;

            var selectedPerson = (Notas) e.SelectedItem;

            var action = await DisplayActionSheet($"Opciones", "Cancelar", null, "Editar", "Eliminar", "Reproducir audio","Ver foto");

            if(selectedPerson!=null) {
                id_nota=selectedPerson.id_nota;
                audio_record=selectedPerson.audio_record;
                photo_record=selectedPerson.photo_record;
                descripcion=selectedPerson.descripcion;
                fecha=selectedPerson.fecha.ToString();
            }

            var firebaseInstance = Singleton.Instance;
            switch (action)
            {
                case "Editar":
                    var pageUpdate = new Page_update();
                    pageUpdate.SetPersonaSeleccionada(selectedPerson);
                    await Navigation.PushAsync(pageUpdate);
                    break;

                case "Eliminar":
                    try
                    {

                    Console.WriteLine("error: "+selectedPerson.id_nota);
                    await firebaseInstance.DeleteData(selectedPerson.id_nota.ToString());
                        await LoadData();
                        await DisplayAlert("Éxito", "Persona eliminada correctamente.", "OK");
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Error", $"Error al eliminar persona: {ex.Message}", "OK");
                    }
                    break;

                case "Reproducir audio":
                    try {
                        ReproducirAudio();
                    } catch(Exception ex) {
                        await DisplayAlert("Error",$"Error {ex.Message}","OK");
                    }
                break;

                case "Ver foto":
                    await Navigation.PushAsync(new Page_photo());
                break;
            }

            peopleListView.SelectedItem = null;
        }

        private void ReproducirAudio() {
            MediaElement mediaElement = new MediaElement {
                Source=audio_record,
                ShouldAutoPlay=true
            };

            container.Add(mediaElement);
        }

        private async Task LoadData()
        {
            try
            {
                var firebaseInstance = Singleton.Instance;

                var personas = await firebaseInstance.ReadData();

                SetPeopleList(new ObservableCollection<Notas>(personas));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al cargar datos: {ex.Message}", "OK");
            }
        }
    }
}
