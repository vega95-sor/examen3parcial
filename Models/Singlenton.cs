using Firebase.Database;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Firebase.Database.Query;
using System.Collections.Generic;
using System.Linq;

namespace Firebase_modelo_singleton.Models{
    public class Singleton{
        private static Singleton instance = null;
        private readonly FirebaseClient firebaseClient;

        private Singleton(){
            firebaseClient = new FirebaseClient("https://examen3-6b4c5-default-rtdb.firebaseio.com/");
        }

        public static Singleton Instance{
            get{
                if (instance == null){
                    instance = new Singleton();
                }

                return instance;
            }
        }

        public async Task CreateData(Notas data){
            await firebaseClient
                .Child("people")  
                .PostAsync(data);
        }

        public async Task<List<Notas>> ReadData(){
            var peopleList = await firebaseClient
                .Child("people")  
                .OnceAsync<Notas>();

            return peopleList.Select(item => {
                var people = item.Object;
                people.id_nota=item.Key; // Asigna el ID de Firebase al objeto people
                return people;
            }).ToList();
        }

        public async Task UpdateData(string key, Notas data){
            await firebaseClient
                .Child("people")  
                .Child(key)
                .PutAsync(data);
        }

        public async Task DeleteData(string key){
            await firebaseClient
                .Child("people")  
                .Child(key)
                .DeleteAsync();
        }
    }
}
