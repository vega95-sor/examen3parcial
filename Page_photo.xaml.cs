namespace Firebase_modelo_singleton;

public partial class Page_photo : ContentPage
{
	public Page_photo()
	{
		InitializeComponent();

		photo.Source=Page_list.photo_record;
	}
}