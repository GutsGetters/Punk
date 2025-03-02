
using System;

namespace Punk.Hotsy
{
    public class HotApp
    {
        private readonly IRecipeMaker _recipeMaker;
        private readonly BuildContext _buildContext;
        private AppDomain _childDomain;
        private Assembly _currentAssembly;

        public HotApp(IRecipeMaker recipeMaker, BuildContext buildContext)
        {
            _recipeMaker = recipeMaker;
            _buildContext = buildContext;

            var hotRecipe = new HotRecipe(recipeMaker);
            hotRecipe.RebuildRequested += OnRebuildRequested;
        }

        private void OnRebuildRequested(object sender, EventArgs e)
        {
            // Запуск пересборки в отдельном потоке
            var builder = new HotBuilder();
            var result = builder.Build(_buildContext, GetSourceFiles());

            if (result.IsSuccess)
            {
                UnloadCurrentDomain();
                LoadNewDomain(result.Value);
            }
        }

        private List<string> GetSourceFiles()
        {
            // Логика получения списка исходных файлов
            return new List<string>();
        }

        private void UnloadCurrentDomain()
        {
            if (_childDomain != null)
            {
                AppDomain.Unload(_childDomain);
                _childDomain = null;
            }
        }

        private void LoadNewDomain(Assembly assembly)
        {
            _childDomain = AppDomain.CreateDomain("ChildDomain");
            _currentAssembly = assembly;
            _childDomain.DoCallBack(() => assembly.EntryPoint.Invoke(null, null));
        }
    }
}
