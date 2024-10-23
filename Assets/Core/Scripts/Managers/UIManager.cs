using UnityEngine;

/// <summary>
/// The UIManager controls all UI windows in the game.
/// </summary>
public class UIManager
{
    public UIWindow currentActiveWindow;

    private Canvas _dynamicCanvas;
    public Canvas DynamicCanvas
    {
        get
        {
            if (_dynamicCanvas == null)
            {
                _dynamicCanvas = GameManager.instance.GetComponentInChildren<Canvas>();
            }
            return _dynamicCanvas;
        }
    }

    private SelectedEnemyWindow _selectedEnemyWindow;
    public SelectedEnemyWindow SelectedEnemyWindow =>
        _selectedEnemyWindow ??= GameObject.FindObjectOfType<SelectedEnemyWindow>(true);

    private TooltipWindow _tooltipWindow;
    public TooltipWindow TooltipWindow =>
        _tooltipWindow ??= GameObject.FindObjectOfType<TooltipWindow>(true);

    private PlayerWindow _playerWindow;
    public PlayerWindow PlayerWindow =>
        _playerWindow ??= GameObject.FindObjectOfType<PlayerWindow>(true);

    private MessageWindow _messageWindow;
    public MessageWindow MessageWindow =>
        _messageWindow ??= GameObject.FindObjectOfType<MessageWindow>(true);

    private CharacterWindow _characterWindow;
    public CharacterWindow CharacterWindow =>
        _characterWindow ??= GameObject.FindObjectOfType<CharacterWindow>(true);

    private InventoryWindow _inventoryWindow;
    public InventoryWindow InventoryWindow =>
        _inventoryWindow ??= GameObject.FindObjectOfType<InventoryWindow>(true);

    private EquipmentWindow _equipmentWindow;
    public EquipmentWindow EquipmentWindow =>
        _equipmentWindow ??= GameObject.FindObjectOfType<EquipmentWindow>(true);

    private StatsWindow _statsWindow;
    public StatsWindow StatsWindow =>
        _statsWindow ??= GameObject.FindObjectOfType<StatsWindow>(true);

    private ShopWindow _shopWindow;
    public ShopWindow ShopWindow =>
        _shopWindow ??= GameObject.FindObjectOfType<ShopWindow>(true);

    private DeathWindow _deathWindow;
    public DeathWindow DeathWindow =>
        _deathWindow ??= GameObject.FindObjectOfType<DeathWindow>(true);

    private NotificationWindow _notificationWindow;
    public NotificationWindow NotificationWindow =>
        _notificationWindow ??= GameObject.FindObjectOfType<NotificationWindow>(true);

    private MainMenuWindow _mainMenuWindow;
    public MainMenuWindow MainMenuWindow =>
        _mainMenuWindow ??= GameObject.FindObjectOfType<MainMenuWindow>(true);

    private OptionsWindow _optionsWindow;
    public OptionsWindow OptionsWindow =>
        _optionsWindow ??= GameObject.FindObjectOfType<OptionsWindow>(true);

    /// <summary>
    /// Sets up all UI windows by calling their respective Setup methods.
    /// </summary>
    public void Setup()
    {
        UIWindow[] windows = GameObject.FindObjectsOfType<UIWindow>(true);
        foreach (UIWindow window in windows)
        {
            window.Setup();
        }
        UIWindow.SetEscapeWindow(MainMenuWindow);
    }
}