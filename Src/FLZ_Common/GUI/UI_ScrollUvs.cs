using UnityEngine;
using UnityEngine.UI;

namespace NOTFGT.FLZ_Common.GUI
{
    internal class UI_ScrollUvs : MonoBehaviour
    {
        const string MAIN_TEX = "_MainTex";
        internal float _ScrollX = 0.15f;
        internal float _ScrollY = 0.15f;

        Image _image;
        Material _mat;

        void Start()
        {
            _image = GetComponent<Image>();

            if (_image == null || _image.sprite == null) Destroy(this);

            _mat = new Material(_image.material);
            _image.material = _mat;

            _mat.mainTexture = _image.sprite.texture;

        }

        void Update()
        {
            _mat.SetTextureOffset(MAIN_TEX, new(_ScrollX * Time.time, _ScrollY * Time.time));
        }
    }
}
