using System;
using System.Collections.Generic;
using System.Text;

namespace UniProject.Share.Domain
{
    /// <summary>
    /// Kiểu cơ sở cho các thực thể miền (domain entities).
    /// Hai thực thể đã được lưu trữ được coi là bằng nhau khi chúng có cùng
    /// kiểu cụ thể và định danh.
    /// </summary>
    public abstract class Entity<TId>
    {
        public TId Id { get; protected set; } = default!;

        public override bool Equals(object? obj)
        {
            if (obj is not Entity<TId> other)
                return false;

            if (obj is null || obj.GetType() != GetType())
                return false;

            if (ReferenceEquals(this, other))
                return true;

            if (EqualityComparer<TId>.Default.Equals(Id, default) || EqualityComparer<TId>.Default.Equals(other.Id, default))
                return false;

            return EqualityComparer<TId>.Default.Equals(Id, other.Id);
        }

        public override int GetHashCode()
        {
            return EqualityComparer<TId>.Default.Equals(Id, default) ? base.GetHashCode() : EqualityComparer<TId>.Default.GetHashCode(Id!);
        }
    }

}
