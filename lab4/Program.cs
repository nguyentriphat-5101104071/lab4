using System;
using System.Collections.Generic;
using System.Linq;

namespace Lab04_ProductManagement
{
    // ==========================================
    // 1. INTERFACE & ENTITY
    // ==========================================
    public interface IEntity
    {
        string Id { get; }
    }

    public class Product : IEntity
    {
        public string MaSP { get; private set; }
        private string tenSP;
        private double price;
        private int quantity;

        // Implement IEntity
        public string Id => MaSP;

        public string TenSP
        {
            get => tenSP;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Tên sản phẩm không được để trống.");
                tenSP = value;
            }
        }

        public double Price
        {
            get => price;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(Price), "Đơn giá không được âm.");
                price = value;
            }
        }

        public int Quantity
        {
            get => quantity;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(Quantity), "Số lượng không được âm.");
                quantity = value;
            }
        }

        public Product(string maSP, string tenSP, double price, int quantity)
        {
            if (string.IsNullOrWhiteSpace(maSP))
                throw new ArgumentException("Mã sản phẩm không được để trống.");

            MaSP = maSP.Trim();
            TenSP = tenSP;
            Price = price;
            Quantity = quantity;
        }

        public override string ToString()
        {
            return $"[Mã: {MaSP,-8} | Tên: {TenSP,-15} | Giá: {Price,10:N0} VNĐ | SL: {Quantity,5} | Tổng trị giá: {Price * Quantity,12:N0} VNĐ]";
        }
    }

    // ==========================================
    // 2. CUSTOM EXCEPTIONS
    // ==========================================
    public class DuplicateProductException : Exception
    {
        public DuplicateProductException(string message) : base(message) { }
    }

    public class ProductNotFoundException : Exception
    {
        public ProductNotFoundException(string message) : base(message) { }
    }

    // ==========================================
    // 3. GENERIC REPOSITORY
    // ==========================================
    public class Repository<T> where T : IEntity
    {
        private readonly List<T> _items = new List<T>();

        public void Add(T item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            // Kiểm tra trùng lặp ID
            if (_items.Any(x => x.Id.Equals(item.Id, StringComparison.OrdinalIgnoreCase)))
            {
                throw new DuplicateProductException($"Lỗi: Đã tồn tại đối tượng có mã '{item.Id}' trong hệ thống.");
            }
            _items.Add(item);
        }

        public bool Remove(string id)
        {
            var item = FindById(id);
            if (item == null)
            {
                throw new ProductNotFoundException($"Lỗi: Không tìm thấy đối tượng có mã '{id}' để xóa.");
            }
            return _items.Remove(item);
        }

        public T FindById(string id)
        {
            return _items.FirstOrDefault(x => x.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        }

        // Sử dụng Func<T, bool> để tìm kiếm linh hoạt theo điều kiện
        public List<T> Find(Func<T, bool> predicate)
        {
            return _items.Where(predicate).ToList();
        }

        public List<T> GetAll()
        {
            return _items.ToList();
        }
    }

    // ==========================================
    // 4. PRODUCT SERVICE WITH EVENTS
    // ==========================================
    public class ProductService
    {
        private readonly Repository<Product> _repository = new Repository<Product>();

        // Định nghĩa các Event sử dụng Action delegate để thông báo khi dữ liệu thay đổi
        public event Action<Product> OnProductAdded;
        public event Action<Product> OnProductRemoved;

        public void AddProduct(Product product)
        {
            _repository.Add(product);
            // Phát event khi thêm thành công
            OnProductAdded?.Invoke(product);
        }

        public void RemoveProduct(string maSP)
        {
            Product prod = _repository.FindById(maSP);
            if (prod == null)
            {
                throw new ProductNotFoundException($"Lỗi: Sản phẩm có mã '{maSP}' không tồn tại.");
            }
            _repository.Remove(maSP);
            // Phát event khi xóa thành công
            OnProductRemoved?.Invoke(prod);
        }

        public Product FindById(string maSP)
        {
            return _repository.FindById(maSP);
        }

        // Tìm sản phẩm có tên chứa từ khóa (không phân biệt hoa thường)
        public List<Product> SearchByName(string keyword)
        {
            return _repository.Find(p => p.TenSP.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        // Lọc theo khoảng giá sử dụng Func<Product, bool>
        public List<Product> FilterByPrice(double minPrice, double maxPrice)
        {
            Func<Product, bool> priceFilter = p => p.Price >= minPrice && p.Price <= maxPrice;
            return _repository.Find(priceFilter);
        }

        public List<Product> GetAllProducts()
        {
            return _repository.GetAll();
        }

        public double CalculateTotalInventoryValue()
        {
            return _repository.GetAll().Sum(p => p.Price * p.Quantity);
        }
    }

    // ==========================================
    // 5. PROGRAM MAIN & MENU
    // ==========================================
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.InputEncoding = System.Text.Encoding.UTF8;

            ProductService productService = new ProductService();

            // Đăng ký các Event lắng nghe sự thay đổi dữ liệu
            productService.OnProductAdded += (prod) =>
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"=> [EVENT] Thêm mới sản phẩm '{prod.TenSP}' (Mã: {prod.MaSP}) thành công!");
                Console.ResetColor();
            };

            productService.OnProductRemoved += (prod) =>
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"=> [EVENT] Đã xóa sản phẩm '{prod.TenSP}' (Mã: {prod.MaSP}) ra khỏi hệ thống!");
                Console.ResetColor();
            };

            // Dữ liệu mẫu ban đầu để dễ test
            try
            {
                productService.AddProduct(new Product("SP01", "Laptop Dell", 15000000, 10));
                productService.AddProduct(new Product("SP02", "iPhone 15 Pro", 28000000, 5));
                productService.AddProduct(new Product("SP03", "Bàn phím cơ", 1500000, 20));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi khởi tạo dữ liệu: " + ex.Message);
            }

            int choice = -1;
            while (choice != 0)
            {
                Console.WriteLine("\n==================== PRODUCT MANAGER ====================");
                Console.WriteLine("1. Thêm sản phẩm");
                Console.WriteLine("2. Xuất danh sách sản phẩm");
                Console.WriteLine("3. Tìm sản phẩm theo mã");
                Console.WriteLine("4. Tìm sản phẩm theo tên");
                Console.WriteLine("5. Lọc sản phẩm theo khoảng giá");
                Console.WriteLine("6. Xóa sản phẩm");
                Console.WriteLine("7. Tính tổng giá trị kho hàng");
                Console.WriteLine("0. Thoát");
                Console.Write("Vui lòng chọn chức năng (0-7): ");

                if (!int.TryParse(Console.ReadLine(), out choice))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Lỗi: Vui lòng nhập một số nguyên hợp lệ.");
                    Console.ResetColor();
                    continue;
                }

                Console.WriteLine("---------------------------------------------------------");
                switch (choice)
                {
                    case 1:
                        ExecuteAddProduct(productService);
                        break;
                    case 2:
                        ExecuteShowAll(productService);
                        break;
                    case 3:
                        ExecuteFindById(productService);
                        break;
                    case 4:
                        ExecuteSearchByName(productService);
                        break;
                    case 5:
                        ExecuteFilterByPrice(productService);
                        break;
                    case 6:
                        ExecuteDeleteProduct(productService);
                        break;
                    case 7:
                        ExecuteShowTotalValue(productService);
                        break;
                    case 0:
                        Console.WriteLine("Tạm biệt! Chương trình kết thúc.");
                        break;
                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Lựa chọn không hợp lệ. Vui lòng chọn từ 0 đến 7.");
                        Console.ResetColor();
                        break;
                }
            }
        }

        // --- HÀM THỰC THI CHO MENU (XỬ LÝ NGOẠI LỆ TRY-CATCH) ---

        static void ExecuteAddProduct(ProductService service)
        {
            Console.WriteLine("[THÊM SẢN PHẨM MỚI]");
            try
            {
                Console.Write("Nhập mã sản phẩm: ");
                string maSP = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(maSP))
                    throw new ArgumentException("Mã sản phẩm không được phép để trống.");

                Console.Write("Nhập tên sản phẩm: ");
                string tenSP = Console.ReadLine();

                Console.Write("Nhập đơn giá: ");
                if (!double.TryParse(Console.ReadLine(), out double price))
                    throw new FormatException("Đơn giá phải là một số thực hợp lệ.");

                Console.Write("Nhập số lượng: ");
                if (!int.TryParse(Console.ReadLine(), out int quantity))
                    throw new FormatException("Số lượng phải là một số nguyên hợp lệ.");

                // Tạo đối tượng Product (constructor sẽ check giá trị âm)
                Product newProduct = new Product(maSP, tenSP, price, quantity);

                // Gọi service để thêm vào Repository (service sẽ check trùng lặp)
                service.AddProduct(newProduct);
            }
            catch (DuplicateProductException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[Lỗi Trùng Lặp]: {ex.Message}");
                Console.ResetColor();
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[Lỗi Phạm Vi]: {ex.Message}");
                Console.ResetColor();
            }
            catch (ArgumentException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[Lỗi Dữ Liệu]: {ex.Message}");
                Console.ResetColor();
            }
            catch (FormatException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[Lỗi Định Dạng]: {ex.Message}");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[Lỗi Không Xác Định]: {ex.Message}");
                Console.ResetColor();
            }
        }

        static void ExecuteShowAll(ProductService service)
        {
            Console.WriteLine("[DANH SÁCH SẢN PHẨM TRONG KHO]");
            var list = service.GetAllProducts();
            if (list.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine("Hiện chưa có sản phẩm nào trong hệ thống.");
                Console.ResetColor();
            }
            else
            {
                foreach (var prod in list)
                {
                    Console.WriteLine(prod);
                }
            }
        }

        static void ExecuteFindById(ProductService service)
        {
            Console.WriteLine("[TÌM SẢN PHẨM THEO MÃ]");
            Console.Write("Nhập mã sản phẩm cần tìm: ");
            string id = Console.ReadLine();

            Product p = service.FindById(id);
            if (p != null)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Tìm thấy sản phẩm:");
                Console.WriteLine(p);
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Không tìm thấy sản phẩm có mã '{id}'.");
                Console.ResetColor();
            }
        }

        static void ExecuteSearchByName(ProductService service)
        {
            Console.WriteLine("[TÌM KIẾM THEO TÊN]");
            Console.Write("Nhập từ khóa tên sản phẩm: ");
            string keyword = Console.ReadLine();

            var results = service.SearchByName(keyword);
            if (results.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"Tìm thấy {results.Count} sản phẩm khớp với từ khóa '{keyword}':");
                Console.ResetColor();
                foreach (var p in results)
                {
                    Console.WriteLine(p);
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Không tìm thấy sản phẩm nào có tên chứa '{keyword}'.");
                Console.ResetColor();
            }
        }

        static void ExecuteFilterByPrice(ProductService service)
        {
            Console.WriteLine("[LỌC THEO KHOẢNG GIÁ]");
            try
            {
                Console.Write("Nhập mức giá nhỏ nhất (Min Price): ");
                if (!double.TryParse(Console.ReadLine(), out double min))
                    throw new FormatException("Đơn giá không hợp lệ.");

                Console.Write("Nhập mức giá lớn nhất (Max Price): ");
                if (!double.TryParse(Console.ReadLine(), out double max))
                    throw new FormatException("Đơn giá không hợp lệ.");

                if (min > max)
                {
                    Console.WriteLine("Giá trị Min không thể lớn hơn Max. Chương trình tự động đổi chỗ.");
                    double temp = min;
                    min = max;
                    max = temp;
                }

                var results = service.FilterByPrice(min, max);
                if (results.Count > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"Tìm thấy {results.Count} sản phẩm trong khoảng giá {min:N0} - {max:N0} VNĐ:");
                    Console.ResetColor();
                    foreach (var p in results)
                    {
                        Console.WriteLine(p);
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine($"Không tìm thấy sản phẩm nào trong khoảng giá từ {min:N0} đến {max:N0} VNĐ.");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Lỗi: {ex.Message}");
                Console.ResetColor();
            }
        }

        static void ExecuteDeleteProduct(ProductService service)
        {
            Console.WriteLine("[XÓA SẢN PHẨM]");
            Console.Write("Nhập mã sản phẩm cần xóa: ");
            string id = Console.ReadLine();

            try
            {
                service.RemoveProduct(id);
            }
            catch (ProductNotFoundException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[Lỗi Xóa]: {ex.Message}");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[Lỗi]: {ex.Message}");
                Console.ResetColor();
            }
        }

        static void ExecuteShowTotalValue(ProductService service)
        {
            Console.WriteLine("[TỔNG GIÁ TRỊ KHO HÀNG]");
            double total = service.CalculateTotalInventoryValue();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Tổng giá trị toàn bộ sản phẩm trong kho: {total:N0} VNĐ");
            Console.ResetColor();
        }
    }
}
