# MVVM Anti-Patterns Reference

## 1. Logic in Code-Behind

### Bad: Logic in Code-Behind

```csharp
// MainPage.xaml.cs
public partial class MainPage : Page
{
    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var service = new ProductService(); // Direct instantiation
        await service.SaveAsync(new Product
        {
            Name = NameTextBox.Text,
            Price = decimal.Parse(PriceTextBox.Text)
        });
        MessageBox.Show("Saved!");
    }
}
```

### Good: Logic in Code-Behind

```csharp
// MainPage.xaml.cs
public partial class MainPage : Page
{
    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}

// MainViewModel.cs
public partial class MainViewModel(IProductService productService) : ObservableObject
{
    [RelayCommand]
    private async Task SaveAsync()
    {
        await productService.SaveAsync(new Product { Name = Name, Price = Price });
    }
}
```

## 2. ViewModel Referencing View Types

### Bad: ViewModel Referencing View Types

```csharp
public class MainViewModel : ObservableObject
{
    private readonly MainPage _page; // Direct View reference

    public void UpdateUI()
    {
        _page.StatusLabel.Text = "Updated"; // Manipulating View directly
    }
}
```

### Good: ViewModel Referencing View Types

```csharp
public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _statusText = string.Empty;
}
```

## 3. Manual INotifyPropertyChanged

### Bad: Manual INotifyPropertyChanged

```csharp
public class ProductViewModel : INotifyPropertyChanged
{
    private string _name;
    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
```

### Good: Manual INotifyPropertyChanged

```csharp
public partial class ProductViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;
}
```

## 4. God ViewModel

### Bad: God ViewModel

```csharp
public partial class MainViewModel : ObservableObject
{
    // 500+ lines handling products, orders, users, settings, navigation, etc.
    [ObservableProperty] private ObservableCollection<Product> _products;
    [ObservableProperty] private ObservableCollection<Order> _orders;
    [ObservableProperty] private User _currentUser;
    [ObservableProperty] private AppSettings _settings;
    // ... hundreds more properties and commands
}
```

### Good: God ViewModel

```csharp
// Split into focused ViewModels
public partial class ProductListViewModel : ObservableObject { /* Products only */ }
public partial class OrderListViewModel : ObservableObject { /* Orders only */ }
public partial class UserProfileViewModel : ObservableObject { /* User only */ }
public partial class SettingsViewModel : ObservableObject { /* Settings only */ }
```

## 5. Direct Service Calls in View

### Bad: Direct Service Calls in View

```xml
<Button Click="OnClick" />
```

```csharp
private async void OnClick(object sender, EventArgs e)
{
    var service = App.ServiceProvider.GetService<IDataService>();
    var data = await service.GetDataAsync();
    // ...
}
```

### Good: Direct Service Calls in View

```xml
<Button Command="{Binding LoadDataCommand}" />
```

```csharp
public partial class DataViewModel(IDataService service) : ObservableObject
{
    [RelayCommand]
    private async Task LoadDataAsync()
    {
        var data = await service.GetDataAsync();
        // ...
    }
}
```

## 6. Exposing Model Directly

### Bad: Exposing Model Directly

```csharp
public partial class OrderViewModel : ObservableObject
{
    [ObservableProperty]
    private Order _order; // Exposes database entity directly

    // View binds to Order.Customer.Address.City
}
```

### Good: Exposing Model Directly

```csharp
public partial class OrderViewModel : ObservableObject
{
    private readonly Order _order;

    public string CustomerName => _order.Customer.Name;
    public string ShippingAddress => FormatAddress(_order.Customer.Address);
    public decimal Total => _order.Items.Sum(i => i.Price * i.Quantity);
}
```

## 7. Synchronous Operations on UI Thread

### Bad: Synchronous Operations on UI Thread

```csharp
[RelayCommand]
private void LoadData()
{
    var data = _service.GetData(); // Blocking call
    Items = new ObservableCollection<Item>(data);
}
```

### Good: Synchronous Operations on UI Thread

```csharp
[RelayCommand]
private async Task LoadDataAsync()
{
    var data = await _service.GetDataAsync();
    Items = new ObservableCollection<Item>(data);
}
```

## 8. Missing Validation

### Bad: Missing Validation

```csharp
[RelayCommand]
private async Task SaveAsync()
{
    await _service.SaveAsync(new Product { Name = Name, Price = Price });
}
```

### Good: Missing Validation

```csharp
public partial class ProductViewModel : ObservableValidator
{
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required]
    private string _name = string.Empty;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        ValidateAllProperties();
        if (!HasErrors)
        {
            await _service.SaveAsync(new Product { Name = Name, Price = Price });
        }
    }

    private bool CanSave() => !HasErrors;
}
```

## 9. Tight Coupling Between ViewModels

### Bad: Tight Coupling Between ViewModels

```csharp
public class OrderViewModel : ObservableObject
{
    private readonly CustomerViewModel _customerViewModel; // Direct reference

    public void SelectCustomer()
    {
        _customerViewModel.ShowSelectionDialog();
        Customer = _customerViewModel.SelectedCustomer;
    }
}
```

### Good: Tight Coupling Between ViewModels

```csharp
public partial class OrderViewModel : ObservableRecipient
{
    public OrderViewModel()
    {
        IsActive = true;
    }

    protected override void OnActivated()
    {
        Messenger.Register<CustomerSelectedMessage>(this, (r, m) =>
        {
            Customer = m.Customer;
        });
    }
}
```

## 10. Not Using IDisposable

### Bad: Not Using IDisposable

```csharp
public class LiveDataViewModel : ObservableObject
{
    private readonly Timer _timer;

    public LiveDataViewModel()
    {
        _timer = new Timer(UpdateData, null, 0, 1000);
    }
    // Timer never stopped, memory leak
}
```

### Good: Not Using IDisposable

```csharp
public partial class LiveDataViewModel : ObservableObject, IDisposable
{
    private readonly Timer _timer;
    private bool _disposed;

    public LiveDataViewModel()
    {
        _timer = new Timer(UpdateData, null, 0, 1000);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _timer.Dispose();
            _disposed = true;
        }
    }
}
```
