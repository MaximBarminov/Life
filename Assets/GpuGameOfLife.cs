using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class GpuGameOfLife : MonoBehaviour
{
    public Font Font;
    public Material Material;

    private const int SpawnPatternRadius = 4;

    private readonly List<Neighborhood> _neighborhoods = new();
    private readonly List<Rule> _rules = new();
    private readonly HashSet<Vector2Int> _spawnPattern = new();
    private readonly StringBuilder _stringBuilder = new();

    private Texture2D _backgroundColorTexture;
    private Color32[] _colors;
    private GUIStyle _containerStyle;
    private int _currentNeighborhoodIndex;
    private int _currentRuleIndex;
    private bool _darkMode = true;
    private bool _decoy;
    private Texture2D _disabledColorTexture;
    private float _elapsedTime;
    private Texture2D _foregroundColorTexture;
    private Vector3 _mousePositionOrigin;
    private Vector2 _offset;
    private Vector2 _offsetOrigin;
    private float _offsetTime;
    private GUIStyle _offStyle;
    private GUIStyle _onStyle;
    private bool _paused;
    private bool _rainbowMode;
    private RenderTexture _renderTexture;
    private RenderTexture _renderTexture2;
    private GUIStyle _rowStyle;
    private int _scale = 2;
    private (int, int)? _screenSize;
    private bool _showUI = true;
    private GUIStyle _textAreaStyle;
    private Texture2D _texture;
    private float _timeScale = 1;

    public Color BackgroundColor => _darkMode ? Color.black : Color.white;

    public Neighborhood CurrentNeighborhood => _neighborhoods[_currentNeighborhoodIndex];

    public Rule CurrentRule => _rules[_currentRuleIndex];

    public Color ForegroundColor => _darkMode ? Color.white : Color.black;

    protected void OnGUI()
    {
        {
            var width = _renderTexture.width * _scale;
            var height = _renderTexture.height * _scale;
            var x = Screen.width / 2 + _offset.x - width / 2;
            var y = Screen.height / 2 + _offset.y - height / 2;
            GUI.DrawTexture(new Rect(x, y, width, height), _renderTexture);
        }

        if (_showUI)
        {
            GUILayout.BeginVertical(_containerStyle);

            _stringBuilder.Clear();
            _stringBuilder.AppendLine($"[Z] toggle UI");
            _stringBuilder.AppendLine();
            _stringBuilder.AppendLine($"[X] toggle dark mode: {ToOnOff(_darkMode)}");
            _stringBuilder.AppendLine($"[C] toggle rainbow mode: {ToOnOff(_rainbowMode)}");
            _stringBuilder.AppendLine($"[V] toggle decoy: {ToOnOff(_decoy)}");
            _stringBuilder.AppendLine();
            _stringBuilder.AppendLine($"[←] [→] rule: {CurrentRule} ({_currentRuleIndex + 1}/{_rules.Count})");
            _stringBuilder.AppendLine();
            _stringBuilder.AppendLine($"[↓] [↑] speed: {_timeScale}x");
            _stringBuilder.AppendLine($"[SPACE] toggle pause");
            _stringBuilder.AppendLine();
            _stringBuilder.AppendLine($"[N] neighborhood: {CurrentNeighborhood} ({_currentNeighborhoodIndex + 1}/{_neighborhoods.Count})");
            _stringBuilder.AppendLine();
            _stringBuilder.AppendLine($"[RMB] pan, twice to reset");
            _stringBuilder.AppendLine($"[SCROLL WHEEL] zoom: {_scale}x");
            _stringBuilder.AppendLine();
            _stringBuilder.AppendLine($"[1] [2] [3] [4] [5] spawn");
            _stringBuilder.AppendLine($"Spawn pattern, [R] to reset:");

            GUILayout.TextArea(_stringBuilder.ToString().Trim(), _textAreaStyle);

            GUILayout.Space(10);

            for (var y = SpawnPatternRadius; y >= -SpawnPatternRadius; y--)
            {
                GUILayout.BeginHorizontal(_rowStyle);
                for (var x = -SpawnPatternRadius; x <= SpawnPatternRadius; x++)
                {
                    var xy = new Vector2Int(x, y);
                    var contains = _spawnPattern.Contains(xy);
                    if (GUILayout.Button(string.Empty, contains ? _onStyle : _offStyle, GUILayout.Width(16), GUILayout.Height(16)))
                    {
                        if (contains)
                        {
                            _spawnPattern.Remove(xy);
                        }
                        else
                        {
                            _spawnPattern.Add(xy);
                        }
                    }
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
        }

        static string ToOnOff(bool flag) => flag ? "on" : "off";
    }

    protected void Start()
    {
        InvalidateTexture();

        _rules.Add(new Rule("B3/S23", "Life"));
        _rules.Add(new Rule("B1/S012345678", "H-trees"));
        _rules.Add(new Rule("B3/S12345", "Maze"));
        _rules.Add(new Rule("B3/S1234", "Mazectric"));
        _rules.Add(new Rule("B35678/S5678", "Diamoeba"));
        _rules.Add(new Rule("B345/S4567", "Assimilation"));
        _rules.Add(new Rule("B2/S", "Seeds"));
        _rules.Add(new Rule("B234/S", "Serviettes"));
        _rules.Add(new Rule("B3/S45678", "Coral"));
        _rules.Add(new Rule("B25/S4"));
        _rules.Add(new Rule("B3/S012345678", "Life without Death"));
        _rules.Add(new Rule("B34/S34", "34 Life"));
        _rules.Add(new Rule("B1357/S1357", "Replicator"));
        _rules.Add(new Rule("B36/S125", "2x2"));
        _rules.Add(new Rule("B36/S23", "HighLife"));
        _rules.Add(new Rule("B3678/S34678", "Day & Night"));
        _rules.Add(new Rule("B368/S245", "Morley"));
        _rules.Add(new Rule("B4678/S35678", "Anneal"));
        _rules.Add(new Rule("B1/S134567", "Snakeskin"));
        _rules.Add(new Rule("B1/S1", "Gnarl"));
        _rules.Add(new Rule("B38/S238", "HoneyLife"));
        _rules.Add(new Rule("B3678/S135678", "Castles"));

        _neighborhoods.Add(new Neighborhood("Moore", 1, 1, 1, 1, 0, 1, 1, 1, 1));
        _neighborhoods.Add(new Neighborhood("Von Neumann", 0, 1, 0, 1, 0, 1, 0, 1, 0));

        InitializeSpawnPattern();

        _backgroundColorTexture = new Texture2D(1, 1);
        _foregroundColorTexture = new Texture2D(1, 1);
        _disabledColorTexture = new Texture2D(1, 1);

        _containerStyle = new GUIStyle { padding = new RectOffset(10, 10, 10, 10) };
        _containerStyle.normal.background = _backgroundColorTexture;

        _textAreaStyle = new GUIStyle { font = Font, fontSize = 14 };

        _rowStyle = new GUIStyle { margin = new RectOffset(10, 0, 0, 0) };

        _onStyle = new GUIStyle { margin = new RectOffset(0, 1, 0, 1) };
        _onStyle.normal.background = _foregroundColorTexture;

        _offStyle = new GUIStyle { margin = new RectOffset(0, 1, 0, 1) };
        _offStyle.normal.background = _disabledColorTexture;

        InvalidateTheme();
    }

    protected void Update()
    {
        InvalidateTexture();

        if (Input.GetKeyDown(KeyCode.Z))
            _showUI ^= true;

        if (Input.GetKeyDown(KeyCode.X))
        {
            _darkMode ^= true;
            InvalidateTheme();
        }

        if (Input.GetKeyDown(KeyCode.C))
            _rainbowMode ^= true;

        if (Input.GetKeyDown(KeyCode.V))
            _decoy ^= true;

        if (Input.GetKeyDown(KeyCode.Space))
            _paused ^= true;

        if (Input.GetKeyDown(KeyCode.R))
            InitializeSpawnPattern();

        if (Input.GetKeyDown(KeyCode.Alpha1))
            SpawnCenter();

        if (Input.GetKeyDown(KeyCode.Alpha2))
            SpawnRandom(0.0001f);

        if (Input.GetKeyDown(KeyCode.Alpha3))
            SpawnRandom(0.001f);

        if (Input.GetKeyDown(KeyCode.Alpha4))
            SpawnRandom(0.01f);

        if (Input.GetKeyDown(KeyCode.Alpha5))
            SpawnRandom(0.1f);

        if (Input.GetKeyDown(KeyCode.RightArrow))
            Next(ref _currentRuleIndex, 1, _rules);

        if (Input.GetKeyDown(KeyCode.LeftArrow))
            Next(ref _currentRuleIndex, -1, _rules);

        if (Input.GetKeyDown(KeyCode.UpArrow))
            _timeScale *= 2f;

        if (Input.GetKeyDown(KeyCode.DownArrow))
            _timeScale /= 2f;

        if (Input.GetKeyDown(KeyCode.N))
            Next(ref _currentNeighborhoodIndex, 1, _neighborhoods);

        if (Input.mouseScrollDelta.y > 0)
            _scale *= 2;

        if (Input.mouseScrollDelta.y < 0)
            _scale /= 2;

        _scale = Mathf.Max(_scale, 1);

        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            if (Time.time - _offsetTime < 0.5f)
                _offset = new Vector2();

            _offsetTime = Time.time;

            _offsetOrigin = _offset;
            _mousePositionOrigin = Input.mousePosition;
        }

        if (Input.GetKey(KeyCode.Mouse1))
        {
            _offset = _offsetOrigin + Vector2.Scale(Input.mousePosition - _mousePositionOrigin, new Vector2(1, -1));
        }

        if (!_paused)
        {
            _elapsedTime += Time.deltaTime * _timeScale;
            if (_elapsedTime > 0.05f)
            {
                Graphics.Blit(_renderTexture, _renderTexture2, Material);
                (_renderTexture2, _renderTexture) = (_renderTexture, _renderTexture2);
                _elapsedTime = 0f;
            }
        }

        Shader.SetGlobalVector("_MainTexSize", new Vector4(1.0f / _texture.width, 1.0f / _texture.height));
        Shader.SetGlobalFloatArray("_N", CurrentNeighborhood.N);
        Shader.SetGlobalFloatArray("_B", CurrentRule.B);
        Shader.SetGlobalFloatArray("_S", CurrentRule.S);
        Shader.SetGlobalColor("_ForegroundColor", GetForegroundColor());
        Shader.SetGlobalFloat("_Decoy", _decoy ? 1 : 0);
    }

    private Color GetForegroundColor()
    {
        return _rainbowMode ? Color.HSVToRGB(Time.time % 1, 1, 1) : ForegroundColor;
    }

    private void InitializeColors(Action spawnAction = null)
    {
        for (var i = 0; i < _colors.Length; i++)
            _colors[i] = Color.clear;

        spawnAction?.Invoke();

        _texture.SetPixels32(_colors);
        _texture.Apply();

        Graphics.Blit(_texture, _renderTexture);
    }

    private void InitializeSpawnPattern()
    {
        _spawnPattern.Clear();
        _spawnPattern.Add(new Vector2Int(0, 0));
    }

    private void InvalidateTexture()
    {
        var screenSize = (Screen.width, Screen.height);
        if (screenSize == _screenSize || !Application.isFocused)
            return;

        if (_screenSize != null)
        {
            Destroy(_texture);
            Destroy(_renderTexture);
            Destroy(_renderTexture2);
        }

        _screenSize = screenSize;

        _texture = new Texture2D(Screen.width / 2, Screen.height / 2)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Repeat
        };

        _renderTexture = new RenderTexture(_texture.width, _texture.height, 16)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Repeat
        };

        _renderTexture2 = new RenderTexture(_texture.width, _texture.height, 16)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Repeat
        };

        _colors = new Color32[_texture.width * _texture.height];
    }

    private void InvalidateTheme()
    {
        GetComponent<Camera>().backgroundColor = BackgroundColor;

        _backgroundColorTexture.SetPixel(0, 0, BackgroundColor);
        _backgroundColorTexture.Apply();

        _foregroundColorTexture.SetPixel(0, 0, ForegroundColor);
        _foregroundColorTexture.Apply();

        _disabledColorTexture.SetPixel(0, 0, Color.Lerp(ForegroundColor, BackgroundColor, 0.8f));
        _disabledColorTexture.Apply();

        _textAreaStyle.normal.textColor = ForegroundColor;

        InitializeColors();
    }

    private void Next<T>(ref int index, int deltaIndex, List<T> list)
    {
        index = ((index + deltaIndex) % list.Count + list.Count) % list.Count;
    }

    private void Spawn(int x, int y)
    {
        foreach (var p in _spawnPattern)
        {
            var px = x + p.x;
            var py = y + p.y;

            var i = px + py * _texture.width;
            if (i >= 0 && i < _colors.Length)
                _colors[i] = GetForegroundColor();
        }
    }

    private void SpawnCenter()
    {
        InitializeColors(() => { Spawn(_texture.width / 2, _texture.height / 2); });
    }

    private void SpawnRandom(float p)
    {
        InitializeColors(() =>
        {
            var count = _texture.width * _texture.height * p / _spawnPattern.Count;
            for (var i = 0; i < count; i++)
                Spawn(UnityEngine.Random.Range(0, _texture.width), UnityEngine.Random.Range(0, _texture.height));
        });
    }

    public class Neighborhood
    {
        public readonly float[] N = new float[9];

        private readonly string _name;

        public Neighborhood(string name, params float[] n)
        {
            _name = name;
            Array.Copy(n, N, n.Length);
        }

        public override string ToString()
        {
            return _name;
        }
    }

    public class Rule
    {
        public readonly float[] B = new float[9];
        public readonly float[] S = new float[9];

        private readonly string _name;
        private readonly string _notation;

        public Rule(string notation, string name = null)
        {
            _notation = notation;
            _name = name;

            var items = _notation.Split('/');

            Parse(items[0], B);
            Parse(items[1], S);

            static void Parse(string input, float[] output)
            {
                var hashSet = new HashSet<int>();

                for (var i = 1; i < input.Length; i++)
                    hashSet.Add(int.Parse(input[i].ToString()));

                for (var i = 0; i < output.Length; i++)
                    if (hashSet.Contains(i))
                        output[i] = 1;
            }
        }

        public override string ToString()
        {
            return _name != null ? _notation + " - " + _name : _notation;
        }
    }
}