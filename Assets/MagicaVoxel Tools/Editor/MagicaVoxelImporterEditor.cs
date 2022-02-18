using System.Collections.Generic;
using UnityEditor;

using UnityEngine;

namespace MagicaVoxelTools
{
    [CustomEditor(typeof(MagicaVoxelImporter))]
    public class MagicaVoxelImporterEditor : UnityEditor.AssetImporters.ScriptedImporterEditor
    {
        private enum ImportType
        {
            Single,
            Multi,
            Merge
        }

        private enum ImportCollider
        {
            None,
            Box,
            Sphere,
            Cylinder,
            Mesh
        }

        private MagicaVoxel importer = new MagicaVoxel();

        private string importAssetName;
        private string importAssetPath;

        private string prefix;
        
        //Prefab ImportSettings
        private ImportType importType = ImportType.Single;

        private ImportCollider importCollider = ImportCollider.None;
        private float importScale = .1f;
        private bool importMaterials = false;
        private Vector3 importPivot = Vector3.zero;
        private bool importStatic = false;
        private bool import2Sided = true;
        private int importPadding = 1;
        private int importTextureScale = 1;
        private bool importRemoveHidden = false;
        private bool importRigidbody = false;
        private bool importSingleMesh = false;

        private Shader importDiffuseShader = null;
        private Shader importMetalShader = null;
        private Shader importGlassShader = null;
        private Shader importEmissionShader = null;
        private Shader importPlasticShader = null;
        private Shader importCloudsShader = null;

        private void LoadSettings()
        {
            string shaderNameDiffuse = EditorPrefs.GetString("VOXIMPORT_importDiffuseShader", "Standard");
            string shaderNameMetal = EditorPrefs.GetString("VOXIMPORT_importMetalShader", "Standard");
            string shaderNameGlass = EditorPrefs.GetString("VOXIMPORT_importGlassShader", "Standard");
            string shaderNameEmission = EditorPrefs.GetString("VOXIMPORT_importEmissionShader", "Standard");
            string shaderNamePlastic = EditorPrefs.GetString("VOXIMPORT_importPlasticShader", "Standard");
            string shaderNameClouds = EditorPrefs.GetString("VOXIMPORT_importCloudsShader", "Standard");

            importDiffuseShader = Shader.Find(shaderNameDiffuse);
            importMetalShader = Shader.Find(shaderNameMetal);
            importGlassShader = Shader.Find(shaderNameGlass);
            importEmissionShader = Shader.Find(shaderNameEmission);
            importPlasticShader = Shader.Find(shaderNamePlastic);
            importCloudsShader = Shader.Find(shaderNameClouds);
            VerifyShaders();

            importScale = EditorPrefs.GetFloat("VOXIMPORT_SCALE", .1f);
            importMaterials = EditorPrefs.GetBool("VOXIMPORT_MAT", false);
            importRemoveHidden = EditorPrefs.GetBool("VOXIMPORT_HIDDEN", false);
            importRigidbody = EditorPrefs.GetBool("VOXIMPORT_RIGIDBODY", false);
            importStatic = EditorPrefs.GetBool("VOXIMPORT_STATIC", false);
            import2Sided = EditorPrefs.GetBool("VOXIMPORT_SHADOW2SIDED", true);
            importSingleMesh = EditorPrefs.GetBool("VOXIMPORT_SINGLEMESH", false);
            importPadding = EditorPrefs.GetInt("VOXIMPORT_TEXUREPAD", 1);
            importTextureScale = EditorPrefs.GetInt("VOXIMPORT_TEXURESCALE", 1);

            importCollider = (ImportCollider)EditorPrefs.GetInt("VOXIMPORT_COLLIDER", 0);
        }

        private void SaveSettings()
        {
            EditorPrefs.SetString("VOXIMPORT_importDiffuseShader", importDiffuseShader.name);
            EditorPrefs.SetString("VOXIMPORT_importMetalShader", importMetalShader.name);
            EditorPrefs.SetString("VOXIMPORT_importGlassShader", importGlassShader.name);
            EditorPrefs.SetString("VOXIMPORT_importEmissionShader", importEmissionShader.name);
            EditorPrefs.SetString("VOXIMPORT_importPlasticShader", importPlasticShader.name);
            EditorPrefs.SetString("VOXIMPORT_importCloudsShader", importCloudsShader.name);

            EditorPrefs.SetFloat("VOXIMPORT_SCALE", importScale);
            EditorPrefs.SetBool("VOXIMPORT_MAT", importMaterials);
            EditorPrefs.SetBool("VOXIMPORT_HIDDEN", importRemoveHidden);
            EditorPrefs.SetBool("VOXIMPORT_RIGIDBODY", importRigidbody);
            EditorPrefs.SetBool("VOXIMPORT_STATIC", importStatic);
            EditorPrefs.SetBool("VOXIMPORT_SHADOW2SIDED", import2Sided);
            EditorPrefs.SetBool("VOXIMPORT_SINGLEMESH", importSingleMesh);
            EditorPrefs.SetInt("VOXIMPORT_TEXUREPAD", importPadding);
            EditorPrefs.SetInt("VOXIMPORT_TEXURESCALE", importTextureScale);
            EditorPrefs.SetInt("VOXIMPORT_COLLIDER", (int)importCollider);
        }

        private void DefaultSettings()
        {
            EditorPrefs.DeleteKey("VOXIMPORT_importDiffuseShader");
            EditorPrefs.DeleteKey("VOXIMPORT_importMetalShader");
            EditorPrefs.DeleteKey("VOXIMPORT_importGlassShader");
            EditorPrefs.DeleteKey("VOXIMPORT_importEmissionShader");

            EditorPrefs.DeleteKey("VOXIMPORT_SCALE");
            EditorPrefs.DeleteKey("VOXIMPORT_MAT");
            EditorPrefs.DeleteKey("VOXIMPORT_HIDDEN");
            EditorPrefs.DeleteKey("VOXIMPORT_RIGIDBODY");
            EditorPrefs.DeleteKey("VOXIMPORT_STATIC");
            EditorPrefs.DeleteKey("VOXIMPORT_SHADOW2SIDED");
            EditorPrefs.DeleteKey("VOXIMPORT_TEXUREPAD");
            EditorPrefs.DeleteKey("VOXIMPORT_TEXURESCALE");
            EditorPrefs.DeleteKey("VOXIMPORT_COLLIDER");
            EditorPrefs.DeleteKey("VOXIMPORT_SINGLEMESH");
            LoadSettings();
        }

        //Preview
        private PreviewRenderUtility preview;

        private float previewRotationX = 0;
        private float previewRotationY = 0;
        private int selectedModel = 0;

        private Mesh previewMesh;
        private Material previewMaterial;
        private Texture2D previewTexture;

        private Mesh previewPivotMesh;
        private Material previewPivotMaterial;

        private void ShowWait()
        {
            EditorUtility.DisplayProgressBar("Please Wait", "Working...", 1);
        }

        private void ClearWait()
        {
            EditorUtility.ClearProgressBar();
        }

        public override void OnEnable()
        {
            base.OnEnable();

            ShowWait();

            LoadSettings();

            preview = new PreviewRenderUtility();

            previewMesh = CreatePrimitiveMesh(PrimitiveType.Cube);
            previewPivotMesh = CreatePrimitiveMesh(PrimitiveType.Sphere);
            //previewMaterial = new Material(Shader.Find("Mobile/Diffuse"));
            previewMaterial = new Material(Shader.Find("Particles/Standard Surface"));

            previewPivotMaterial = new Material(Shader.Find("GUI/Text Shader"));
            previewPivotMaterial.color = Color.red;

            string path = AssetDatabase.GetAssetPath(target);
            importAssetName = path.Substring(path.LastIndexOf('/') + 1);
            importAssetPath = path.Substring(0, path.Length - importAssetName.Length);
            importAssetName = importAssetName.Substring(0, importAssetName.Length - 4);
            //Debug.Log(path);
            //Debug.Log(importAssetPath);
            //Debug.Log(importAssetName);

            importer.LoadFile(path);

            if (importer.IsValid)
            {
                CreatePreviewMesh(importer.GetData(0));
                CenterPivot();
            }

            ClearWait();
        }

        public override void OnDisable()
        {
            base.OnDisable();
            ClearWait();

            SaveSettings();
            preview.Cleanup();
            preview = null;
        }

        public override void OnInspectorGUI()
        {
            ApplyRevertGUI();

            if (!importer.IsValid)
            {
                EditorGUILayout.LabelField("Invalid Vox file!");
                return;
            }

            if (importer.models.Count == 1)
            {
                EditorGUILayout.BeginHorizontal();
                string info = string.Format("{0} Voxels {1}", importer.totalVoxels, importer.Size(selectedModel).ToString());
                EditorGUILayout.LabelField(info);
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUI.BeginChangeCheck();
                importType = (ImportType)EditorGUILayout.EnumPopup(importType, GUILayout.Width(58));
                if (EditorGUI.EndChangeCheck())
                {
                    if (importType == ImportType.Merge)
                    {
                        CreatePreviewMesh(MergedModels());
                        CenterPivot();
                    }
                    else
                    {
                        CreatePreviewMesh(importer.GetData(selectedModel));
                        CenterPivot();
                    }
                }

                if (importType == ImportType.Merge)
                {
                    Vector3Int previewSize = new Vector3Int((int)previewMesh.bounds.size.x, (int)previewMesh.bounds.size.y, (int)previewMesh.bounds.size.z);
                    string info = string.Format("{0} Models, {1} Voxels {2}", importer.models.Count, importer.totalVoxels, previewSize.ToString());
                    EditorGUILayout.LabelField(info);
                }
                else
                {
                    EditorGUI.BeginChangeCheck();
                    selectedModel = EditorGUILayout.Popup(selectedModel, importer.modelNames.ToArray());
                    if (EditorGUI.EndChangeCheck())
                    {
                        CreatePreviewMesh(importer.GetData(selectedModel));
                        CenterPivot();
                    }

                    string info = string.Format("{0} Models {1}", importer.models.Count, importer.Size(selectedModel).ToString());
                    EditorGUILayout.LabelField(info);
                }

                EditorGUILayout.EndHorizontal();
            }

            HandlePreview();

            importScale = EditorGUILayout.FloatField("Voxel Scale", importScale);
            importPadding = EditorGUILayout.IntSlider("Texture Padding", importPadding, 0, 8);
            importTextureScale = EditorGUILayout.IntSlider("Texture Scale", importTextureScale, 1, 16);

            GUILayout.BeginHorizontal();
            importPivot = EditorGUILayout.Vector3Field("Pivot", importPivot);
            if (GUILayout.Button("Center", GUILayout.Width(50))) CenterPivot();
            GUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            importStatic = EditorGUILayout.Toggle("Static", importStatic);
            importRemoveHidden = EditorGUILayout.Toggle("Remove Hidden", importRemoveHidden);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            importMaterials = EditorGUILayout.Toggle("Materials", importMaterials);
            importSingleMesh = EditorGUILayout.Toggle("Single Mesh", importSingleMesh);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            import2Sided = EditorGUILayout.Toggle("2 Sided", import2Sided);
            EditorGUILayout.EndHorizontal();

            importRigidbody = EditorGUILayout.Toggle("Rigid Body", importRigidbody);

            importCollider = (ImportCollider)EditorGUILayout.EnumPopup("Collider", importCollider);

            EditorGUILayout.BeginHorizontal();

            GUILayout.Label("Prefix",GUILayout.Width(40));
            prefix = GUILayout.TextField(prefix, GUILayout.Width(75));

            if (GUILayout.Button("Create Prefab"))
            {
                CreatePrefab();
            }

            if (GUILayout.Button("Create Voxel Data"))
            {
                CreateData();
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Default Settings"))
            {
                DefaultSettings();
            }

            if (importMaterials)
            {
                GUILayout.Space(10);
                //EditorGUILayout.HelpBox("Materials", MessageType.None);
                importDiffuseShader = (Shader)EditorGUILayout.ObjectField("Diffuse", importDiffuseShader, typeof(Shader), true);
                importMetalShader = (Shader)EditorGUILayout.ObjectField("Metal", importMetalShader, typeof(Shader), true);
                importGlassShader = (Shader)EditorGUILayout.ObjectField("Glass", importGlassShader, typeof(Shader), true);
                importEmissionShader = (Shader)EditorGUILayout.ObjectField("Emission", importEmissionShader, typeof(Shader), true);
                importPlasticShader = (Shader)EditorGUILayout.ObjectField("Plastic", importPlasticShader, typeof(Shader), true);
                importCloudsShader = (Shader)EditorGUILayout.ObjectField("Clouds", importCloudsShader, typeof(Shader), true);
            }
        }

        private void CreatePrefab()
        {
            ShowWait();

            if (importType == ImportType.Single)
            {
                VoxelData data = importer.GetData(selectedModel);
                if (importRemoveHidden) RemoveHidden(data);

                if (importSingleMesh)
                    CreatePrefabSingle(data, prefix + importAssetName + "_" + importer.modelNames[selectedModel]);
                else
                    CreatePrefabMulti(data, prefix + importAssetName + "_" + importer.modelNames[selectedModel]);
            }
            if (importType == ImportType.Multi)
            {
                for (int i = 0; i < importer.models.Count; i++)
                {
                    VoxelData data = importer.GetData(i);
                    if (importRemoveHidden) RemoveHidden(data);

                    if (importSingleMesh)
                        CreatePrefabSingle(data, prefix + importAssetName + "_" + importer.modelNames[i]);
                    else
                        CreatePrefabMulti(data, prefix + importAssetName + "_" + importer.modelNames[i]);
                }
            }
            if (importType == ImportType.Merge)
            {
                VoxelData data = MergedModels();
                if (importRemoveHidden) RemoveHidden(data);

                if (importSingleMesh)
                    CreatePrefabSingle(data, prefix + importAssetName + "_merged");
                else
                    CreatePrefabMulti(data, prefix + importAssetName + "_merged");
            }

            ClearWait();
        }

        private void CreateData()
        {
            ShowWait();

            if (importType == ImportType.Single)
            {
                VoxelData data = importer.GetData(selectedModel);
                if (importRemoveHidden) RemoveHidden(data);
                CreateData(data, prefix + importAssetName + "_" + importer.modelNames[selectedModel] + "_data");
            }
            if (importType == ImportType.Multi)
            {
                for (int i = 0; i < importer.models.Count; i++)
                {
                    VoxelData data = importer.GetData(i);
                    if (importRemoveHidden) RemoveHidden(data);
                    CreateData(data, prefix + importAssetName + "_" + importer.modelNames[i] + "_data");
                }
            }
            if (importType == ImportType.Merge)
            {
                VoxelData data = MergedModels();
                if (importRemoveHidden) RemoveHidden(data);
                CreateData(data, prefix + importAssetName + "_merged" + "_data");
            }

            ClearWait();
        }

        private void HandlePreview()
        {
            Rect previewRect = GUILayoutUtility.GetRect(EditorGUIUtility.currentViewWidth, 256 + 100);

            if (Event.current.type == EventType.MouseDrag)
            {
                if (previewRect.Contains(Event.current.mousePosition))
                {
                    previewRotationX -= Event.current.delta.y;
                    previewRotationY -= Event.current.delta.x;
                }
                Repaint();
            }

            DrawRenderPreview(previewRect);
        }

        private void DrawRenderPreview(Rect previewRect)
        {
            Quaternion rotation = Quaternion.Euler(previewRotationX, previewRotationY, 0);
            Matrix4x4 matrix = Matrix4x4.identity;
            matrix.SetTRS(Vector3.zero, rotation, Vector3.one);
            Vector3 position = -matrix.MultiplyPoint(previewMesh.bounds.center);
            Vector3 position2 = matrix.MultiplyPoint(importPivot - previewMesh.bounds.center);

            float dist = previewMesh.bounds.extents.magnitude * 2;

            preview.camera.transform.position = new Vector3(0, 0, -dist);
            preview.camera.transform.LookAt(Vector3.zero);
            preview.camera.clearFlags = CameraClearFlags.Color;
            preview.camera.backgroundColor = Color.gray;
            preview.camera.fieldOfView = 60;
            preview.camera.nearClipPlane = .3f;
            preview.camera.farClipPlane = 10000f;

            preview.BeginPreview(previewRect, GUIStyle.none);

            preview.DrawMesh(previewMesh, position, rotation, previewMaterial, 0);
            preview.DrawMesh(previewPivotMesh, position2, rotation, previewPivotMaterial, 0);

            preview.Render();
            preview.EndAndDrawPreview(previewRect);
        }

        private void CenterPivot()
        {
            importPivot = previewMesh.bounds.size / 2;
        }

        private static Mesh CreatePrimitiveMesh(PrimitiveType type)
        {
            GameObject gameObject = GameObject.CreatePrimitive(type);
            Mesh mesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
            GameObject.DestroyImmediate(gameObject);
            return mesh;
        }

        private VoxelData MergedModels()
        {
            Vector3Int min = new Vector3Int(4096, 4096, 4096);
            Vector3Int max = new Vector3Int(-4096, -4096, -4096);

            for (int i = 0; i < importer.models.Count; i++)
            {
                Vector3Int s = importer.Size(i);
                Vector3Int c = importer.modelOffsets[i];//centers

                int xmin = c.x - (s.x / 2);
                int xmax = c.x + (s.x / 2);
                if (min.x > xmin) min.x = xmin;
                if (max.x < xmax) max.x = xmax;

                int ymin = c.y - (s.y / 2);
                int ymax = c.y + (s.y / 2);
                if (min.y > ymin) min.y = ymin;
                if (max.y < ymax) max.y = ymax;

                int zmin = c.z - (s.z / 2);
                int zmax = c.z + (s.z / 2);
                if (min.z > zmin) min.z = zmin;
                if (max.z < zmax) max.z = zmax;
            }

            //Debug.Log(min.ToString());
            //Debug.Log(max.ToString());

            int w = Mathf.Abs(max.x - min.x) + 1;
            int h = Mathf.Abs(max.y - min.y) + 1;
            int d = Mathf.Abs(max.z - min.z) + 1;
            //Debug.Log(w + "," + h + "," + d);

            VoxelData data = new VoxelData(w, h, d, importer.palette, importer.materials);

            //Merge all models...
            for (int i = 0; i < importer.models.Count; i++)
            {
                Vector3Int s = importer.Size(i);
                Vector3Int o = importer.modelOffsets[i];
                o.x -= min.x;
                o.y -= min.y;
                o.z -= min.z;

                o.x -= s.x / 2;
                o.y -= s.y / 2;
                o.z -= s.z / 2;

                for (int x = 0; x < s.x; x++)
                {
                    for (int y = 0; y < s.y; y++)
                    {
                        for (int z = 0; z < s.z; z++)
                        {
                            if (importer.models[i][x, y, z] > 0)
                            {
                                data.SetVoxel(x + o.x, y + o.y, z + o.z, importer.models[i][x, y, z]);
                            }
                        }
                    }
                }
            }

            return data;
        }

        private void RemoveHidden(VoxelData data)
        {
            for (int x = 0; x < data.width; x++)
            {
                for (int y = 0; y < data.height; y++)
                {
                    for (int z = 0; z < data.depth; z++)
                    {
                        if (!data.IsVisible(x, y, z))
                        {
                            data.SetVoxel(x, y, z, 0);
                        }
                    }
                }
            }
        }

        private void CreatePreviewMesh(VoxelData data)
        {
            ShowWait();

            PreviewVoxelMesh pm = new PreviewVoxelMesh(data);
            previewMesh = pm.mesh;

            ClearWait();
        }

        private void VerifyShaders()
        {
            //Fallback to Standard Shaders...
            if (importDiffuseShader == null) importDiffuseShader = Shader.Find("Standard");
            if (importMetalShader == null) importMetalShader = Shader.Find("Standard");
            if (importGlassShader == null) importGlassShader = Shader.Find("Standard");
            if (importEmissionShader == null) importEmissionShader = Shader.Find("Standard");
            if (importPlasticShader == null) importPlasticShader = Shader.Find("Standard");
            if (importCloudsShader == null) importCloudsShader = Shader.Find("Standard");
        }

        private void CreateData(VoxelData data, string pfName)
        {
            string assetPath = importAssetPath + pfName + ".asset";

            AssetDatabase.CreateAsset(data, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public void CreatePrefabMulti(VoxelData data, string pfName)
        {
            VoxelData dd = new VoxelData(data);
            VerifyShaders();

            GameObject obj = new GameObject(pfName);
            obj.isStatic = importStatic;

            OptimizedVoxelMesh assetDifuse = null;
            Material diffuseMat = new Material(importDiffuseShader);
            diffuseMat.name = "Diffuse";

            OptimizedVoxelMesh assetGlass = null;
            Material glassMat = new Material(importGlassShader);
            glassMat.name = "Glass";

            OptimizedVoxelMesh assetMetal = null;
            Material metalMat = new Material(importMetalShader);
            metalMat.name = "Metal";

            OptimizedVoxelMesh assetEmission = null;
            Material emissionMat = new Material(importEmissionShader);
            emissionMat.name = "Emission";

            OptimizedVoxelMesh assetPlastic = null;
            Material plasticMat = new Material(importPlasticShader);
            plasticMat.name = "Plastic";

            OptimizedVoxelMesh assetClouds = null;
            Material cloudsMat = new Material(importCloudsShader);
            cloudsMat.name = "Clouds";

            if (importMaterials)
            {
                dd.ClearByMat(VoxMaterialType._glass, true);
                dd.ClearByMat(VoxMaterialType._metal, true);
                dd.ClearByMat(VoxMaterialType._emit, true);
                dd.ClearByMat(VoxMaterialType._plastic, true);
                dd.ClearByMat(VoxMaterialType._clouds, true);
            }

            if (dd.VoxelCount() > 0)
            {
                assetDifuse = new OptimizedVoxelMesh(dd, importPivot, importTextureScale, importPadding, importScale);
                assetDifuse.mesh.name = "Diffuse";
                assetDifuse.texture.name = "Diffuse";

                GameObject diffuse = new GameObject("Diffuse");
                diffuse.transform.parent = obj.transform;
                diffuse.isStatic = importStatic;

                MeshFilter mf = diffuse.AddComponent<MeshFilter>();
                mf.mesh = assetDifuse.mesh;

                diffuseMat.mainTexture = assetDifuse.texture;
                diffuseMat.SetFloat("_Metallic", 0);
                diffuseMat.SetFloat("_Glossiness", 0);

                MeshRenderer mr = diffuse.AddComponent<MeshRenderer>();
                if (import2Sided) mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;

                mr.material = diffuseMat;

                AddCollider(diffuse);
                if (importRigidbody) diffuse.AddComponent<Rigidbody>();
            }

            if (importMaterials)
            {
                VoxelData gd = new VoxelData(data);
                gd.ClearByMat(VoxMaterialType._glass, false);
                if (gd.VoxelCount() > 0)
                {
                    glassMat.SetFloat("_Glossiness", 1);
                    glassMat.SetInt("_Mode", 3);//transparent
                    glassMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    glassMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    glassMat.SetInt("_ZWrite", 0);
                    glassMat.DisableKeyword("_ALPHATEST_ON");
                    glassMat.DisableKeyword("_ALPHABLEND_ON");
                    glassMat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                    glassMat.renderQueue = 3000;
                    glassMat.color = new Color(1, 1, 1, 0);

                    assetGlass = new OptimizedVoxelMesh(gd, importPivot, importTextureScale, importPadding, importScale);
                    assetGlass.mesh.name = "Glass";

                    GameObject glass = new GameObject("Glass");
                    glass.transform.parent = obj.transform;
                    glass.isStatic = false;//glass should not be lightmapped

                    MeshFilter glassmf = glass.AddComponent<MeshFilter>();
                    MeshRenderer glassmr = glass.AddComponent<MeshRenderer>();
                    if (import2Sided) glassmr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;

                    glassmf.mesh = assetGlass.mesh;
                    glassmr.material = glassMat;

                    AddCollider(glass);
                    if (importRigidbody) glass.AddComponent<Rigidbody>();
                }

                VoxelData md = new VoxelData(data);
                md.ClearByMat(VoxMaterialType._metal, false);
                if (md.VoxelCount() > 0)
                {
                    assetMetal = new OptimizedVoxelMesh(md, importPivot, importTextureScale, importPadding, importScale);
                    assetMetal.mesh.name = "Metal";
                    assetMetal.texture.name = "Metal";

                    GameObject metal = new GameObject("Metal");
                    metal.transform.parent = obj.transform;
                    metal.isStatic = importStatic;

                    MeshFilter metalmf = metal.AddComponent<MeshFilter>();
                    MeshRenderer metalmr = metal.AddComponent<MeshRenderer>();
                    if (import2Sided) metalmr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;

                    metalMat.mainTexture = assetMetal.texture;
                    metalMat.SetFloat("_Metallic", 1);
                    metalMat.SetFloat("_Glossiness", 1);
                    metalMat.SetFloat("_GlossyReflections", 1);

                    metalmf.mesh = assetMetal.mesh;
                    metalmr.material = metalMat;

                    AddCollider(metal);
                    if (importRigidbody) metal.AddComponent<Rigidbody>();
                }

                VoxelData ed = new VoxelData(data);
                ed.ClearByMat(VoxMaterialType._emit, false);
                if (ed.VoxelCount() > 0)
                {
                    assetEmission = new OptimizedVoxelMesh(ed, importPivot, importTextureScale, importPadding, importScale);
                    assetEmission.mesh.name = "Emission";
                    assetEmission.texture.name = "Emission";

                    emissionMat.SetTexture("_EmissionMap", assetEmission.texture);
                    emissionMat.EnableKeyword("_EMISSION");
                    emissionMat.SetColor("_EmissionColor", Color.white);
                    emissionMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

                    GameObject emit = new GameObject("Emission");
                    emit.transform.parent = obj.transform;
                    emit.isStatic = importStatic;

                    MeshFilter emitmf = emit.AddComponent<MeshFilter>();
                    MeshRenderer emitmr = emit.AddComponent<MeshRenderer>();
                    if (import2Sided) emitmr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;

                    emitmf.mesh = assetEmission.mesh;
                    emitmr.material = emissionMat;

                    AddCollider(emit);
                    if (importRigidbody) emit.AddComponent<Rigidbody>();
                }

                VoxelData pd = new VoxelData(data);
                pd.ClearByMat(VoxMaterialType._plastic, false);
                if (pd.VoxelCount() > 0)
                {
                    assetPlastic = new OptimizedVoxelMesh(pd, importPivot, importTextureScale, importPadding, importScale);
                    assetPlastic.mesh.name = "Plastic";
                    assetPlastic.texture.name = "Plastic";

                    plasticMat.mainTexture = assetPlastic.texture;
                    plasticMat.SetFloat("_Metallic", 0);
                    plasticMat.SetFloat("_Glossiness", 1);

                    GameObject plastic = new GameObject("Plastic");
                    plastic.transform.parent = obj.transform;
                    plastic.isStatic = importStatic;

                    MeshFilter plasticmf = plastic.AddComponent<MeshFilter>();
                    MeshRenderer plasticmr = plastic.AddComponent<MeshRenderer>();
                    if (import2Sided) plasticmr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;

                    plasticmf.mesh = assetPlastic.mesh;
                    plasticmr.material = plasticMat;

                    AddCollider(plastic);
                    if (importRigidbody) plastic.AddComponent<Rigidbody>();
                }

                VoxelData cd = new VoxelData(data);
                cd.ClearByMat(VoxMaterialType._clouds, false);
                if (cd.VoxelCount() > 0)
                {
                    assetClouds = new OptimizedVoxelMesh(cd, importPivot, importTextureScale, importPadding, importScale);
                    assetClouds.mesh.name = "Clouds";
                    assetClouds.texture.name = "Clouds";

                    cloudsMat.mainTexture = assetClouds.texture;
                    cloudsMat.SetFloat("_Glossiness", 0);
                    cloudsMat.SetInt("_Mode", 3);//transparent
                    cloudsMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    cloudsMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    cloudsMat.SetInt("_ZWrite", 0);
                    cloudsMat.DisableKeyword("_ALPHATEST_ON");
                    cloudsMat.DisableKeyword("_ALPHABLEND_ON");
                    cloudsMat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                    cloudsMat.renderQueue = 3000;
                    cloudsMat.color = new Color(1, 1, 1, .5f);

                    GameObject clouds = new GameObject("Clouds");
                    clouds.transform.parent = obj.transform;
                    clouds.isStatic = importStatic;

                    MeshFilter cloudsmf = clouds.AddComponent<MeshFilter>();
                    MeshRenderer cloudsmr = clouds.AddComponent<MeshRenderer>();
                    if (import2Sided) cloudsmr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;

                    cloudsmf.mesh = assetClouds.mesh;
                    cloudsmr.material = cloudsMat;

                    AddCollider(clouds);
                    if (importRigidbody) clouds.AddComponent<Rigidbody>();
                }
            }

            //create prefab

            if (assetDifuse != null || assetGlass != null || assetMetal != null || assetEmission != null || assetPlastic != null || assetClouds != null)
            {
                string assetPath = importAssetPath + pfName + ".asset";
                string prefabPath = importAssetPath + pfName + ".prefab";

                AssetDatabase.CreateAsset(new Mesh(), assetPath);

                if (assetDifuse != null)
                {
                    AssetDatabase.AddObjectToAsset(assetDifuse.mesh, assetPath);
                    AssetDatabase.AddObjectToAsset(assetDifuse.texture, assetPath);
                    AssetDatabase.AddObjectToAsset(diffuseMat, assetPath);
                }
                if (assetGlass != null)
                {
                    AssetDatabase.AddObjectToAsset(assetGlass.mesh, assetPath);
                    AssetDatabase.AddObjectToAsset(glassMat, assetPath);
                }
                if (assetMetal != null)
                {
                    AssetDatabase.AddObjectToAsset(assetMetal.mesh, assetPath);
                    AssetDatabase.AddObjectToAsset(assetMetal.texture, assetPath);
                    AssetDatabase.AddObjectToAsset(metalMat, assetPath);
                }
                if (assetEmission != null)
                {
                    AssetDatabase.AddObjectToAsset(assetEmission.mesh, assetPath);
                    AssetDatabase.AddObjectToAsset(assetEmission.texture, assetPath);
                    AssetDatabase.AddObjectToAsset(emissionMat, assetPath);
                }
                if (assetPlastic != null)
                {
                    AssetDatabase.AddObjectToAsset(assetPlastic.mesh, assetPath);
                    AssetDatabase.AddObjectToAsset(assetPlastic.texture, assetPath);
                    AssetDatabase.AddObjectToAsset(plasticMat, assetPath);
                }
                if (assetClouds != null)
                {
                    AssetDatabase.AddObjectToAsset(assetClouds.mesh, assetPath);
                    AssetDatabase.AddObjectToAsset(assetClouds.texture, assetPath);
                    AssetDatabase.AddObjectToAsset(cloudsMat, assetPath);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                PrefabUtility.CreatePrefab(prefabPath, obj);
                //GameObject prefab = PrefabUtility.CreatePrefab(prefabPath, obj);
                //Selection.activeObject = prefab;
            }

            GameObject.DestroyImmediate(obj);
        }

        public void CreatePrefabSingle(VoxelData data, string pfName)
        {
            VoxelData dd = new VoxelData(data);
            VerifyShaders();

            GameObject obj = new GameObject(pfName);
            obj.isStatic = importStatic;

            OptimizedVoxelMesh assetDifuse = null;
            Material diffuseMat = new Material(importDiffuseShader);
            diffuseMat.name = "Diffuse";

            OptimizedVoxelMesh assetGlass = null;
            Material glassMat = new Material(importGlassShader);
            glassMat.name = "Glass";

            OptimizedVoxelMesh assetMetal = null;
            Material metalMat = new Material(importMetalShader);
            metalMat.name = "Metal";

            OptimizedVoxelMesh assetEmission = null;
            Material emissionMat = new Material(importEmissionShader);
            emissionMat.name = "Emission";

            OptimizedVoxelMesh assetPlastic = null;
            Material plasticMat = new Material(importPlasticShader);
            plasticMat.name = "Plastic";

            OptimizedVoxelMesh assetClouds = null;
            Material cloudsMat = new Material(importCloudsShader);
            cloudsMat.name = "Clouds";

            if (importMaterials)
            {
                dd.ClearByMat(VoxMaterialType._glass, true);
                dd.ClearByMat(VoxMaterialType._metal, true);
                dd.ClearByMat(VoxMaterialType._emit, true);
                dd.ClearByMat(VoxMaterialType._plastic, true);
                dd.ClearByMat(VoxMaterialType._clouds, true);
            }

            if (dd.VoxelCount() > 0)
            {
                assetDifuse = new OptimizedVoxelMesh(dd, importPivot, importTextureScale, importPadding, importScale);
                assetDifuse.mesh.name = "Diffuse";
                assetDifuse.texture.name = "Diffuse";

                diffuseMat.mainTexture = assetDifuse.texture;
                diffuseMat.SetFloat("_Metallic", 0);
                diffuseMat.SetFloat("_Glossiness", 0);
            }

            if (importMaterials)
            {
                VoxelData gd = new VoxelData(data);
                gd.ClearByMat(VoxMaterialType._glass, false);
                if (gd.VoxelCount() > 0)
                {
                    assetGlass = new OptimizedVoxelMesh(gd, importPivot, importTextureScale, importPadding, importScale);
                    assetGlass.mesh.name = "Glass";

                    glassMat.SetFloat("_Glossiness", 1);
                    glassMat.SetInt("_Mode", 3);//transparent
                    glassMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    glassMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    glassMat.SetInt("_ZWrite", 0);
                    glassMat.DisableKeyword("_ALPHATEST_ON");
                    glassMat.DisableKeyword("_ALPHABLEND_ON");
                    glassMat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                    glassMat.renderQueue = 3000;
                    glassMat.color = new Color(1, 1, 1, 0);
                }

                VoxelData md = new VoxelData(data);
                md.ClearByMat(VoxMaterialType._metal, false);
                if (md.VoxelCount() > 0)
                {
                    assetMetal = new OptimizedVoxelMesh(md, importPivot, importTextureScale, importPadding, importScale);
                    assetMetal.mesh.name = "Metal";
                    assetMetal.texture.name = "Metal";

                    metalMat.mainTexture = assetMetal.texture;
                    metalMat.SetFloat("_Metallic", 1);
                    metalMat.SetFloat("_Glossiness", 1);
                    metalMat.SetFloat("_GlossyReflections", 1);
                }

                VoxelData ed = new VoxelData(data);
                ed.ClearByMat(VoxMaterialType._emit, false);
                if (ed.VoxelCount() > 0)
                {
                    assetEmission = new OptimizedVoxelMesh(ed, importPivot, importTextureScale, importPadding, importScale);
                    assetEmission.mesh.name = "Emission";
                    assetEmission.texture.name = "Emission";

                    emissionMat.SetTexture("_EmissionMap", assetEmission.texture);
                    emissionMat.EnableKeyword("_EMISSION");
                    emissionMat.SetColor("_EmissionColor", Color.white);
                    emissionMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                }

                VoxelData pd = new VoxelData(data);
                pd.ClearByMat(VoxMaterialType._plastic, false);
                if (pd.VoxelCount() > 0)
                {
                    assetPlastic = new OptimizedVoxelMesh(pd, importPivot, importTextureScale, importPadding, importScale);
                    assetPlastic.mesh.name = "Plastic";
                    assetPlastic.texture.name = "Plastic";

                    plasticMat.mainTexture = assetPlastic.texture;
                    plasticMat.SetFloat("_Metallic", 0);
                    plasticMat.SetFloat("_Glossiness", 1);
                }

                VoxelData cd = new VoxelData(data);
                cd.ClearByMat(VoxMaterialType._clouds, false);
                if (cd.VoxelCount() > 0)
                {
                    assetClouds = new OptimizedVoxelMesh(cd, importPivot, importTextureScale, importPadding, importScale);
                    assetClouds.mesh.name = "Clouds";
                    assetClouds.texture.name = "Clouds";

                    cloudsMat.mainTexture = assetClouds.texture;
                    cloudsMat.SetFloat("_Metallic", 0);
                    cloudsMat.SetFloat("_Glossiness", 0);
                    cloudsMat.SetInt("_Mode", 3);//transparent
                    cloudsMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    cloudsMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    cloudsMat.SetInt("_ZWrite", 0);
                    cloudsMat.DisableKeyword("_ALPHATEST_ON");
                    cloudsMat.DisableKeyword("_ALPHABLEND_ON");
                    cloudsMat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                    cloudsMat.renderQueue = 3000;
                    cloudsMat.color = new Color(1, 1, 1, .5f);
                }
            }

            Mesh objMesh = new Mesh();
            int subMesh = 0;

            List<Vector3> buildVerts = new List<Vector3>();
            List<Vector2> buildUvs = new List<Vector2>();
            Dictionary<int, List<int>> buildIdxs = new Dictionary<int, List<int>>();
            List<Material> mats = new List<Material>();

            if (assetDifuse != null)
            {
                buildIdxs.Add(subMesh, AddMesh(ref assetDifuse.mesh, ref buildVerts, ref buildUvs));
                mats.Add(diffuseMat);
                subMesh++;
            }

            if (assetGlass != null)
            {
                buildIdxs.Add(subMesh, AddMesh(ref assetGlass.mesh, ref buildVerts, ref buildUvs));
                mats.Add(glassMat);
                subMesh++;
            }

            if (assetMetal != null)
            {
                buildIdxs.Add(subMesh, AddMesh(ref assetMetal.mesh, ref buildVerts, ref buildUvs));
                mats.Add(metalMat);
                subMesh++;
            }

            if (assetEmission != null)
            {
                buildIdxs.Add(subMesh, AddMesh(ref assetEmission.mesh, ref buildVerts, ref buildUvs));
                mats.Add(emissionMat);
                subMesh++;
            }

            if (assetPlastic != null)
            {
                buildIdxs.Add(subMesh, AddMesh(ref assetPlastic.mesh, ref buildVerts, ref buildUvs));
                mats.Add(plasticMat);
                subMesh++;
            }

            if (assetClouds != null)
            {
                buildIdxs.Add(subMesh, AddMesh(ref assetClouds.mesh, ref buildVerts, ref buildUvs));
                mats.Add(cloudsMat);
                subMesh++;
            }

            objMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            objMesh.vertices = buildVerts.ToArray();
            objMesh.uv = buildUvs.ToArray();

            objMesh.subMeshCount = subMesh;
            for (int i=0; i < subMesh; i++)
            {
                int[] mi = buildIdxs[i].ToArray();
                //Debug.Log(i + " - " + mi.Length);
                objMesh.SetIndices(mi, MeshTopology.Triangles, i);
            }

            objMesh.RecalculateNormals();

            MeshFilter objMf = obj.AddComponent<MeshFilter>();
            MeshRenderer objMr = obj.AddComponent<MeshRenderer>();
            //objMf.mesh = objMesh;
            //objMr.materials = mats.ToArray();
            objMf.sharedMesh = objMesh;
            objMr.sharedMaterials = mats.ToArray();

            if (import2Sided) objMr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
            AddCollider(obj);
            if (importRigidbody) obj.AddComponent<Rigidbody>();



            //create prefab
            if (assetDifuse != null || assetGlass != null || assetMetal != null || assetEmission != null || assetPlastic != null || assetClouds != null)
            {
                string assetPath = importAssetPath + pfName + ".asset";
                string prefabPath = importAssetPath + pfName + ".prefab";

                AssetDatabase.CreateAsset(new Mesh(), assetPath);

                AssetDatabase.AddObjectToAsset(objMesh, assetPath);

                if (assetDifuse != null)
                {
                    AssetDatabase.AddObjectToAsset(assetDifuse.texture, assetPath);
                    AssetDatabase.AddObjectToAsset(diffuseMat, assetPath);
                }
                if (assetGlass != null)
                {
                    AssetDatabase.AddObjectToAsset(glassMat, assetPath);
                }
                if (assetMetal != null)
                {
                    AssetDatabase.AddObjectToAsset(assetMetal.texture, assetPath);
                    AssetDatabase.AddObjectToAsset(metalMat, assetPath);
                }
                if (assetEmission != null)
                {
                    AssetDatabase.AddObjectToAsset(assetEmission.texture, assetPath);
                    AssetDatabase.AddObjectToAsset(emissionMat, assetPath);
                }
                if (assetPlastic != null)
                {
                    AssetDatabase.AddObjectToAsset(assetPlastic.texture, assetPath);
                    AssetDatabase.AddObjectToAsset(plasticMat, assetPath);
                }
                if (assetClouds != null)
                {
                    AssetDatabase.AddObjectToAsset(assetClouds.texture, assetPath);
                    AssetDatabase.AddObjectToAsset(cloudsMat, assetPath);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                PrefabUtility.CreatePrefab(prefabPath, obj);
            }

            GameObject.DestroyImmediate(obj);
        }

        private List<int> AddMesh(ref Mesh source, ref List<Vector3> verts, ref List<Vector2> uvs)
        {
            List<int> idxs = new List<int>();
            int offset = verts.Count;

            Vector3[] v = source.vertices;
            Vector2[] uv = source.uv;

            for (int i=0; i < v.Length; i++)
            {
                verts.Add(v[i]);
                uvs.Add(uv[i]);
            }

            int[] idx = source.GetIndices(0);

            for (int i=0; i < idx.Length; i++)
            {
                idxs.Add(idx[i] + offset);
            }

            return idxs;
        }

        private void AddCollider(GameObject obj)
        {
            switch (importCollider)
            {
                case ImportCollider.None:
                    break;

                case ImportCollider.Box:
                    obj.AddComponent<BoxCollider>();
                    break;

                case ImportCollider.Sphere:
                    obj.AddComponent<SphereCollider>();
                    break;

                case ImportCollider.Mesh:
                    obj.AddComponent<MeshCollider>();
                    break;
            }
        }
    }
}