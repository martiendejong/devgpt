using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevGPT.NewAPI;

namespace Store.OpnieuwOpnieuw.Helpers.FileTree
{
    public class TreeMaker<T>
    {
        public static List<TreeNode<T>> GetTree(Dictionary<string, T> files)
        {
            var nodes = new List<TreeNode<T>>();
            var result = new List<TreeNode<T>>();
            foreach (var file in files)
            {
                var names = file.Key.Split(['/', '\\']);
                TreeNode<T> parentNode = new TreeNode<T>(names[0]);
                result.Add(parentNode);

                for (var i = 1; i < names.Length - 1; ++i)
                {
                    var childNode = nodes.SingleOrDefault(n => n.Name == names[0] && n.Parent == parentNode);
                    if (childNode == null)
                    {
                        childNode = new TreeNode<T>(names[i]);
                        childNode.Parent = parentNode;
                        parentNode.Children.Add(childNode);
                        nodes.Add(parentNode);
                        parentNode = childNode;
                    }
                }
            }
            return result;
        }
    }
}
