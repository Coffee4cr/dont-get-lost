using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class technology : MonoBehaviour
{
    public List<technology> depends_on;
    public Sprite sprite;
    public string description;

    public string display_name => name.Replace('_', ' ');

    public bool researched => tech_tree.research_complete(this);

    public virtual string info()
    {
        string ret = description;

        foreach (var i in Resources.LoadAll<item>("items"))
            if (i.GetComponent<technology_requirement>()?.technology == this)
                ret += "\nUnlocks " + i.display_name;

        return ret;
    }

    public bool complete => tech_tree.research_complete(name);

    public bool prerequisites_complete
    {
        get
        {
            foreach (var t in depends_on)
                if (!t.complete)
                    return false;
            return true;
        }
    }

    public bool materials_available
    {
        get
        {
            foreach (var material in GetComponentsInChildren<research_material_ingredient>())
                if (tech_tree.research_materials_count(material.material) < material.count)
                    return false;
            return true;
        }
    }

    public HashSet<technology> depends_on_set
    {
        get
        {
            if (_depends_on_set == null)
                _depends_on_set = new HashSet<technology>(depends_on);
            return _depends_on_set;
        }
    }
    HashSet<technology> _depends_on_set;

    public bool linked_to(technology other) => depends_on_set.Contains(other) || other.depends_on_set.Contains(this);

    //##############//
    // STATIC STUFF //
    //##############//

    public static technology[] all => Resources.LoadAll<technology>("technologies");

    public static bool is_valid_name(string name)
    {
        foreach (var t in all)
            if (t.name == name)
                return true;
        return false;
    }

    public static technology load(string name)
    {
        foreach (var t in all)
            if (t.name == name)
                return t;
        return null;
    }
}
