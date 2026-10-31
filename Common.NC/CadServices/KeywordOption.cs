using System;

namespace dRz.Common.NC.CadServices
{
    /// <summary>
    /// Описывает один вариант выбора в командной строке CAD.
    /// </summary>
    public class KeywordOption
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KeywordOption"/> class.
        /// </summary>
        /// <param name="globalName">Name of the global.</param>
        /// <param name="localName">Name of the local.</param>
        /// <param name="answer">The answer.</param>
        /// <param name="isDefault">if set to <c>true</c> [is default].</param>
        /// <param name="isVisible">if set to <c>true</c> [is visible].</param>
        /// <param name="isEnabled">if set to <c>true</c> [is enabled].</param>
        public KeywordOption(string globalName,
                        string localName,
                        Enum answer,
                        bool isDefault = false,
                        bool isVisible = true,
                        bool isEnabled = true)
        {
            Answer = answer;

            GlobalName = globalName;
            LocalName = localName;
            DisplayName = localName;

            IsDefault = isDefault;
            IsVisible = isVisible;
            IsEnabled = isEnabled;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeywordOption"/> class.
        /// </summary>
        /// <param name="globalName">Name of the global.</param>
        /// <param name="answer">The answer.</param>
        /// <param name="isDefault">if set to <c>true</c> [is default].</param>
        /// <param name="isVisible">if set to <c>true</c> [is visible].</param>
        /// <param name="isEnabled">if set to <c>true</c> [is enabled].</param>
        public KeywordOption(string globalName,
                        Enum answer,
                        bool isDefault = false,
                        bool isVisible = true,
                        bool isEnabled = true)
        {
            Answer = answer;

            LocalName = answer.DisplayName();
            DisplayName = answer.DisplayName();

            GlobalName = globalName;

            IsDefault = isDefault;
            IsVisible = isVisible;
            IsEnabled = isEnabled;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeywordOption"/> class.
        /// </summary>
        /// <param name="answer">The answer.</param>
        /// <param name="isDefault">if set to <c>true</c> [is default].</param>
        /// <param name="isVisible">if set to <c>true</c> [is visible].</param>
        /// <param name="isEnabled">if set to <c>true</c> [is enabled].</param>
        public KeywordOption(Enum answer,
                        bool isDefault = false,
                        bool isVisible = true,
                        bool isEnabled = true)
        {
            Answer = answer;

            LocalName = answer.DisplayName();
            _displayName = answer.DisplayName();

            GlobalName = answer.ToString();

            IsDefault = isDefault;
            IsVisible = isVisible;
            IsEnabled = isEnabled;
        }

        /// <summary>
        /// Gets or sets the name of the global.
        /// </summary>
        /// <value>
        /// The name of the global.
        /// </value>
        public string GlobalName
        {
            get => _globalName;
            set => _globalName = value;
        }

        /// <summary>
        /// Gets or sets the name of the local.
        /// </summary>
        /// <value>
        /// The name of the local.
        /// </value>
        public string LocalName
        {
            get => _localName;
            set => _localName = value;
        }

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        /// <value>
        /// The display name.
        /// </value>
        public string DisplayName
        {
            get => _displayName;
            set => _displayName = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is default.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is default; otherwise, <c>false</c>.
        /// </value>
        public bool IsDefault
        {
            get => _isDefault;
            set => _isDefault = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is visible.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is visible; otherwise, <c>false</c>.
        /// </value>
        public bool IsVisible
        {
            get => _isVisible;
            set => _isVisible = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is enabled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is enabled; otherwise, <c>false</c>.
        /// </value>
        public bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }

        /// <summary>
        /// Gets or sets the answer.
        /// </summary>
        /// <value>
        /// The answer.
        /// </value>
        public Enum? Answer
        {
            get => _answer;
            set => _answer = value;
        }

        private Enum? _answer;

        private string _globalName = null!;
        private string _localName = null!;
        private string _displayName = null!;

        private bool _isVisible;
        private bool _isEnabled;
        private bool _isDefault;
    }
}