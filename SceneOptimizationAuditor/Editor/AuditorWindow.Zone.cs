using System;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityTools.SceneOptimization
{
    // The player zone of the Viewpoints tab in the Scene view (3.0.0, owner, 29.10.2026: "the zone of the play field for
    // the player, from which the script measures and where it puts the viewpoints"): rectangles always outlined while the
    // tab is open; with "Player Zone" on they are filled, the picked one has handles and a panel in the corner adds,
    // removes and types in the values.
    public sealed partial class AuditorWindow
    {
        const float ZonePanelWidth = 300f;
        // A new rectangle's floor goes this far under the ground found below the Scene view pivot, so uneven ground stays in.
        const float ZoneFloorSlack = 0.5f;

        readonly BoxBoundsHandle zoneBox = new BoxBoundsHandle();
        bool zoneDirty;

        // Turned on from the toolbar or "Set…" / "Edit…" of the placement panel.
        // keep: the settings of the placement panel the zone editing was opened from, so they are not lost.
        internal void EditZone(bool on, PlacementSettings keep = null)
        {
            vpZoneEdit = on;
            var set = Viewpoints();
            if (keep != null)
            {
                set.placement = keep;
                set.Save();
            }
            if (on)
            {
                if (vpZoneSelected < 0 || vpZoneSelected >= set.zone.Count) vpZoneSelected = set.zone.Count > 0 ? 0 : -1;
                var view = SceneView.lastActiveSceneView;
                if (view == null) ShowNotification(new GUIContent(Strings.Get("ui.vp.noSceneView")));
                else view.Focus();
            }
            Repaint();
            SceneView.RepaintAll();
        }

        void DrawZoneInScene(SceneView view, ViewpointSet set)
        {
            if (set.zone.Count == 0 && !vpZoneEdit) return;
            if (Event.current.type == EventType.Repaint)
            {
                var style = new GUIStyle(EditorStyles.whiteMiniLabel) { normal = { background = Texture2D.blackTexture } };
                for (int i = 0; i < set.zone.Count; i++)
                {
                    var r = set.zone[i];
                    bool picked = vpZoneEdit && i == vpZoneSelected;
                    // Filled only while editing; the picked one brighter.
                    var fill = Tint(ZoneColor, !vpZoneEdit ? 0f : picked ? 0.28f : 0.14f);
                    Handles.DrawSolidRectangleWithOutline(r.Corners(0.02f), fill, ZoneColor);
                    if (vpZoneEdit) Handles.DrawSolidRectangleWithOutline(r.Corners(r.height), Tint(ZoneColor, 0f), Tint(ZoneColor, 0.5f));
                    Handles.Label(r.Corners(0.02f)[1], " " + Strings.Format("ui.vp.zone.label", i + 1) + " ", style);
                }
            }
            if (!vpZoneEdit) return;
            for (int i = 0; i < set.zone.Count; i++)
            {
                if (i == vpZoneSelected) continue;
                var r = set.zone[i];
                float size = HandleUtility.GetHandleSize(r.center) * 0.12f;
                Handles.color = ZoneColor;
                if (Handles.Button(r.center, Quaternion.LookRotation(Vector3.up), size, size, Handles.RectangleHandleCap))
                {
                    vpZoneSelected = i;
                    Repaint();
                }
            }
            if (vpZoneSelected >= 0 && vpZoneSelected < set.zone.Count) DrawZoneHandles(set, set.zone[vpZoneSelected]);
            Handles.color = Color.white;
            if (zoneDirty && GUIUtility.hotControl == 0)
            {
                set.Save();
                zoneDirty = false;
                Repaint();
            }
        }

        // Size and height on the faces of the box, the move arrows in the middle of the floor and the turn ring around it.
        void DrawZoneHandles(ViewpointSet set, ZoneRect r)
        {
            var rotation = r.Rotation;
            var middle = r.center + Vector3.up * (r.height * 0.5f);
            using (new Handles.DrawingScope(ZoneColor, Matrix4x4.TRS(middle, rotation, Vector3.one)))
            {
                zoneBox.center = Vector3.zero;
                zoneBox.size = new Vector3(r.size.x, r.height, r.size.y);
                EditorGUI.BeginChangeCheck();
                zoneBox.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(set, "Edit Player Zone");
                    var s = zoneBox.size;
                    var newMiddle = middle + rotation * zoneBox.center;
                    r.size = new Vector2(Mathf.Max(ZoneRect.MinSize, s.x), Mathf.Max(ZoneRect.MinSize, s.z));
                    r.height = Mathf.Max(ZoneRect.MinSize, s.y);
                    r.center = newMiddle - Vector3.up * (r.height * 0.5f);
                    zoneDirty = true;
                }
            }
            EditorGUI.BeginChangeCheck();
            var moved = Handles.PositionHandle(r.center, rotation);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(set, "Edit Player Zone");
                r.center = moved;
                zoneDirty = true;
            }
            float ring = Mathf.Max(r.size.x, r.size.y) * 0.5f * 1.15f;
            Handles.color = WarningColor;
            EditorGUI.BeginChangeCheck();
            var turned = Handles.Disc(rotation, r.center, Vector3.up, ring, false, 15f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(set, "Edit Player Zone");
                r.yaw = Mathf.Repeat(turned.eulerAngles.y, 360f);
                zoneDirty = true;
            }
        }

        float ZonePanelHeight(ViewpointSet set)
        {
            return vpZoneSelected >= 0 && vpZoneSelected < set.zone.Count ? 196f : 118f;
        }

        // The panel in the top left corner of the Scene view while editing.
        void DrawZonePanel(SceneView view, ViewpointSet set)
        {
            if (!vpZoneEdit) return;
            Handles.BeginGUI();
            bool picked = vpZoneSelected >= 0 && vpZoneSelected < set.zone.Count;
            var area = new Rect(10f, 10f, ZonePanelWidth, ZonePanelHeight(set));
            GUILayout.BeginArea(area, EditorStyles.helpBox);
            GUILayout.Label(Strings.Get("ui.vp.zone.title"), EditorStyles.boldLabel);
            GUILayout.Label(set.zone.Count > 0
                ? Strings.Format("ui.vp.zone.count", Strings.Count("ui.vp.zone.rects", set.zone.Count), FormatNumber(Math.Round(set.ZoneArea)))
                : Strings.Get("ui.vp.zone.empty"), wrapMini);
            if (picked)
            {
                var r = set.zone[vpZoneSelected];
                GUILayout.Label(Strings.Format("ui.vp.zone.selected", vpZoneSelected + 1), EditorStyles.miniBoldLabel);
                EditorGUI.BeginChangeCheck();
                EditorGUIUtility.labelWidth = 90f;
                // Without wide mode the two numbers go under the label and the panel runs over its height.
                bool wide = EditorGUIUtility.wideMode;
                EditorGUIUtility.wideMode = true;
                var size = EditorGUILayout.Vector2Field(Strings.Get("ui.vp.zone.size"), r.size);
                EditorGUIUtility.wideMode = wide;
                float yaw = EditorGUILayout.FloatField(Strings.Get("ui.vp.zone.turn"), r.yaw);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(Strings.Get("ui.vp.zone.height"), GUILayout.Width(86f));
                float from = EditorGUILayout.FloatField(r.center.y);
                GUILayout.Label("–", GUILayout.ExpandWidth(false));
                float to = EditorGUILayout.FloatField(r.center.y + r.height);
                EditorGUILayout.EndHorizontal();
                EditorGUIUtility.labelWidth = 0f;
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(set, "Edit Player Zone");
                    r.size = new Vector2(Mathf.Max(ZoneRect.MinSize, size.x), Mathf.Max(ZoneRect.MinSize, size.y));
                    r.yaw = Mathf.Repeat(yaw, 360f);
                    r.center.y = from;
                    r.height = Mathf.Max(ZoneRect.MinSize, to - from);
                    set.Save();
                    SceneView.RepaintAll();
                    Repaint();
                }
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(Strings.Get("ui.vp.zone.add"), EditorStyles.miniButton)) AddZoneRect(view, set);
            EditorGUI.BeginDisabledGroup(!picked);
            if (GUILayout.Button(Strings.Get("ui.vp.zone.remove"), EditorStyles.miniButton, GUILayout.ExpandWidth(false))) RemoveZoneRect(set);
            EditorGUI.EndDisabledGroup();
            if (GUILayout.Button(Strings.Get("ui.vp.zone.done"), EditorStyles.miniButton, GUILayout.ExpandWidth(false))) EditZone(false);
            EditorGUILayout.EndHorizontal();
            GUILayout.EndArea();
            // A click on the panel itself does not select what lies behind it in the scene.
            if (Event.current.type == EventType.MouseDown && area.Contains(Event.current.mousePosition)) Event.current.Use();
            Handles.EndGUI();
        }

        // A 10 × 10 m rectangle on the ground under the Scene view pivot.
        void AddZoneRect(SceneView view, ViewpointSet set)
        {
            Undo.RecordObject(set, "Add Player Zone Rectangle");
            set.zone.Add(NewZoneRect(view.pivot));
            vpZoneSelected = set.zone.Count - 1;
            set.Save();
            SceneView.RepaintAll();
            Repaint();
        }

        // The floor is the highest surface not above the pivot (judge of 3.0.0: the first hit from above was the roof inside
        // a building); without one, the lowest surface above it; without any, the pivot itself.
        public static ZoneRect NewZoneRect(Vector3 pivot)
        {
            Physics.SyncTransforms();
            var hits = Physics.RaycastAll(pivot + Vector3.up * 50f, Vector3.down, 200f, ~0, QueryTriggerInteraction.Ignore);
            float floor = pivot.y;
            var below = hits.Where(h => h.point.y <= pivot.y + 0.01f).ToList();
            if (below.Count > 0) floor = below.Max(h => h.point.y);
            else if (hits.Length > 0) floor = hits.Min(h => h.point.y);
            return new ZoneRect { center = new Vector3(pivot.x, floor - ZoneFloorSlack, pivot.z) };
        }

        void RemoveZoneRect(ViewpointSet set)
        {
            if (vpZoneSelected < 0 || vpZoneSelected >= set.zone.Count) return;
            Undo.RecordObject(set, "Remove Player Zone Rectangle");
            set.zone.RemoveAt(vpZoneSelected);
            vpZoneSelected = Math.Min(vpZoneSelected, set.zone.Count - 1);
            set.Save();
            SceneView.RepaintAll();
            Repaint();
        }
    }
}
