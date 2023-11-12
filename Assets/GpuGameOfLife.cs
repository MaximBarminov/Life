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
    private bool _darkMode;
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
            _stringBuilder.AppendLine($"[F1] toggle UI");
            _stringBuilder.AppendLine();
            _stringBuilder.AppendLine($"[F2] toggle dark mode: {ToOnOff(_darkMode)}");
            _stringBuilder.AppendLine($"[F3] toggle rainbow mode: {ToOnOff(_rainbowMode)}");
            _stringBuilder.AppendLine($"[F4] toggle decoy: {ToOnOff(_decoy)}");
            _stringBuilder.AppendLine();
            _stringBuilder.AppendLine($"[↑] [↓] change rules: {CurrentRule} ({_currentRuleIndex + 1}/{_rules.Count})");
            _stringBuilder.AppendLine();
            _stringBuilder.AppendLine($"[←] [→] change speed: {_timeScale}x");
            _stringBuilder.AppendLine($"[SPACE] toggle pause");
            _stringBuilder.AppendLine();
            _stringBuilder.AppendLine($"[N] change neighborhood: {CurrentNeighborhood} ({_currentNeighborhoodIndex + 1}/{_neighborhoods.Count})");
            _stringBuilder.AppendLine();
            _stringBuilder.AppendLine($"[RMB] pan, twice to reset");
            _stringBuilder.AppendLine($"[SCROLL WHEEL] zoom: {_scale}x");
            _stringBuilder.AppendLine();
            _stringBuilder.AppendLine($"[1] [2] [3] [4] [5] spawn");
            _stringBuilder.AppendLine($"Spawn pattern ([R] reset):");

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

        _rules.Add(RuleBuilder.B(3).S(2, 3));
        _rules.Add(RuleBuilder.B(1).S(0, 1, 2, 3, 4, 5, 6, 7, 8));
        _rules.Add(RuleBuilder.B(3).S(1, 2, 3, 4, 5));
        _rules.Add(RuleBuilder.B(3, 5, 6, 7, 8).S(5, 6, 7, 8));
        _rules.Add(RuleBuilder.B(3, 4, 5).S(4, 5, 6, 7));
        _rules.Add(RuleBuilder.B(2).S());
        _rules.Add(RuleBuilder.B(2, 3, 4).S());
        _rules.Add(RuleBuilder.B(3).S(4, 5, 6, 7, 8));

        _neighborhoods.Add(new Neighborhood("Moore", 1, 1, 1, 1, 0, 1, 1, 1, 1));
        _neighborhoods.Add(new Neighborhood("Von Neumann", 0, 1, 0, 1, 0, 1, 0, 1, 0));

        InitializeSpawnPattern();

        _backgroundColorTexture = new Texture2D(1, 1);
        _foregroundColorTexture = new Texture2D(1, 1);
        _disabledColorTexture = new Texture2D(1, 1);

        _containerStyle = new GUIStyle { padding = new RectOffset(10, 10, 10, 10) };
        _containerStyle.normal.background = _backgroundColorTexture;

        _textAreaStyle = new GUIStyle { font = Font, fontSize = 18 };

        _rowStyle = new GUIStyle { margin = new RectOffset(10, 0, 0, 0) };

        _onStyle = new GUIStyle { margin = new RectOffset(0, 1, 0, 1) };
        _onStyle.normal.background = _foregroundColorTexture;

        _offStyle = new GUIStyle { margin = new RectOffset(0, 1, 0, 1) };
        _offStyle.normal.background = _disabledColorTexture;

        Shader.SetGlobalVector("_MainTexSize", new Vector4(1.0f / _texture.width, 1.0f / _texture.height));

        InvalidateTheme();
    }

    protected void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
            _showUI ^= true;

        if (Input.GetKeyDown(KeyCode.F2))
        {
            _darkMode ^= true;
            InvalidateTheme();
        }

        if (Input.GetKeyDown(KeyCode.F3))
            _rainbowMode ^= true;

        if (Input.GetKeyDown(KeyCode.F4))
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
            SpawnRandom(0.05f);

        if (Input.GetKeyDown(KeyCode.Alpha4))
            SpawnRandom(0.2f);

        if (Input.GetKeyDown(KeyCode.Alpha5))
            SpawnRandom(0.35f);

        if (Input.GetKeyDown(KeyCode.RightArrow))
            _timeScale *= 2f;

        if (Input.GetKeyDown(KeyCode.LeftArrow))
            _timeScale /= 2f;

        if (Input.GetKeyDown(KeyCode.UpArrow))
            Next(ref _currentRuleIndex, 1, _rules);

        if (Input.GetKeyDown(KeyCode.DownArrow))
            Next(ref _currentRuleIndex, -1, _rules);

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

        public Rule(int[] b, int[] s)
        {
            _name = "B";
            Add(b, B, ref _name);
            _name += "/S";
            Add(s, S, ref _name);

            static void Add(int[] input, float[] output, ref string name)
            {
                var hashSet = new HashSet<int>(input);
                for (var i = 0; i < output.Length; i++)
                {
                    if (hashSet.Contains(i))
                    {
                        output[i] = 1;
                        name += i;
                    }
                }
            }
        }

        public override string ToString()
        {
            return _name;
        }
    }

    public class RuleBuilder
    {
        private int[] _b;

        public static RuleBuilder B(params int[] b)
        {
            return new RuleBuilder { _b = b };
        }

        public Rule S(params int[] s)
        {
            return new Rule(_b, s);
        }
    }
}