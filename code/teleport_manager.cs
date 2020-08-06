﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using networked_variables;

public class teleport_manager : networked
{
    // The list of teleport destinations (saved over the network)
    networked_list<networked_pair<net_string, net_vector3>> destinations = new networked_list<networked_pair<net_string, net_vector3>>();

    public void register_portal(portal p)
    {
        var net_name = new net_string("portal");
        var net_pos = new net_vector3(p.path_start.position);
        destinations.add(new networked_pair<net_string, net_vector3>(net_name, net_pos));
    }

    public void unregister_portal(portal p)
    {
        int to_remove = -1;
        for (int i = 0; i < destinations.length; ++i)
            if ((destinations[i].second.value - p.path_start.position).magnitude < 10e-4)
            {
                to_remove = i;
                break;
            }

        if (to_remove < 0) throw new System.Exception("Could not find the portal to unregister!");
        destinations.remove_at(to_remove);
    }

    public void create_buttons(RectTransform parent)
    {
        var btn = Resources.Load<UnityEngine.UI.Button>("ui/teleport_button");
        foreach (var d in destinations)
        {
            var b = btn.inst();
            b.GetComponentInChildren<UnityEngine.UI.Text>().text = d.first.value;
            b.transform.SetParent(parent);

            Vector3 target = d.second.value; // Copy for lambda

            b.onClick.AddListener(() =>
            {
                player.current.teleport(target);
            });
        }
    }

    public override float network_radius()
    {
        // The teleport manager is always loaded
        return float.PositiveInfinity;
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(teleport_manager))]
    new class editor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var tm = (teleport_manager)target;
            string text = "Teleport destinations:\n";
            foreach (var d in tm.destinations)
                text += d.first.value + ": " + d.second.value + "\n";
            UnityEditor.EditorGUILayout.TextArea(text);
        }
    }
#endif
}