namespace dRz.Abstractions.Infrastructure
{
    /// <summary>
    /// Содержит имена ключей метаданных сборки, используемых для описания аддона и его среды выполнения.
    /// </summary>
    public static class AssemblyMetadataKeys
    {
        /// <summary>Адрес электронной почты для общих контактов.</summary>
        public const string ContactEmail = nameof(ContactEmail);

        /// <summary>Адрес электронной почты службы поддержки.</summary>
        public const string SupportEmail = nameof(SupportEmail);

        /// <summary>Адрес электронной почты поставщика.</summary>
        public const string VendorEmail = nameof(VendorEmail);

        /// <summary>Домашняя страница продукта или поставщика.</summary>
        public const string HomePage = nameof(HomePage);

        /// <summary>Семейство CAD-хоста, для которого предназначен аддон.</summary>
        public const string HostFamily = nameof(HostFamily);

        /// <summary>Краткий код CAD-хоста.</summary>
        public const string HostCode = nameof(HostCode);

        /// <summary>Идентификатор семейства продукта.</summary>
        public const string ProductFamily = nameof(ProductFamily);

        /// <summary>Имя поставщика продукта.</summary>
        public const string Vendor = nameof(Vendor);

        /// <summary>URL репозитория исходного кода, определённый SDK.</summary>
        public const string RepositoryUrl = nameof(RepositoryUrl);
    }
}