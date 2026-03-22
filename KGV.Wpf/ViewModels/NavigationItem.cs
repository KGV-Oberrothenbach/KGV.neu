using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace KGV.ViewModels
{
    public class NavigationItem : INotifyPropertyChanged
    {
        private string _title = string.Empty;
        private bool _isVisible = true;
        private int _badgeCount;
        private bool _isAttention;

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public Thickness ButtonMargin { get; set; } = new Thickness(5);

        /// <summary>
        /// Ziel-ViewModel-Typ (ViewModel-first Navigation).
        /// Beispiel: typeof(MemberSearchViewModel), typeof(MemberDetailViewModel), ...
        /// </summary>
        public Type? ViewModelType { get; set; }

        /// <summary>
        /// Optionaler Parameter für Navigation (z.B. MemberDTO, Id, etc.)
        /// </summary>
        public object? Parameter { get; set; }

        public bool IsAdminOnly { get; set; } = false;
        public bool IsVorstandOnly { get; set; } = false;

        /// <summary>
        /// Wird im XAML genutzt, um Buttons ein-/auszublenden.
        /// </summary>
        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        public int BadgeCount
        {
            get => _badgeCount;
            set
            {
                if (SetProperty(ref _badgeCount, value))
                    OnPropertyChanged(nameof(HasBadge));
            }
        }

        public bool HasBadge => BadgeCount > 0;

        public bool IsAttention
        {
            get => _isAttention;
            set => SetProperty(ref _isAttention, value);
        }

        public List<NavigationItem>? SubItems { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}