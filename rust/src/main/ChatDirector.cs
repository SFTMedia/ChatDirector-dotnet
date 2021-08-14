using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Shims;
using System;
using System.Collections.Generic;
namespace Oxide.Ext.ChatDirector.core
{
    public class ChatDirector
    {
        Configuration config = null;
        Configuration configStaging = null;
        static ChatDirector instance;
        string rawData;
        public ChatDirector(string rawData) : this()
        {
            this.rawData = rawData;
        }
        public ChatDirector()
        {
            config = new Configuration();
            instance = this;
            rawData = File.ReadAllText("config.yml");
        }
        public bool loadConfig()
        {
            var contractResolver = new SnakeCaseContractResolver();
            var deserializerSettings = new JsonSerializerSettings();
            deserializerSettings.ContractResolver = contractResolver;
            var deserializer = JsonSerializer.Create(deserializerSettings);
            deserializer.Converters.Add(new EchoItemConverter());
            deserializer.Converters.Add(new ChainConverter());
            deserializer.Converters.Add(new ConfigurationConverter());
            try
            {
                if (rawData != null)
                {
                    configStaging = deserializer.Deserialize<Configuration>(new JsonTextReader(new StringReader(rawData)));
                }
                else
                {
                    Console.WriteLine("No data to parse as config");
                }
            }
            catch (IOException e)
            {
                Console.WriteLine(e.ToString());
                Console.WriteLine("Config failed to load.");
                return false;
            }
            return true;
        }
        public bool load()
        {
            // Load config
            // NOTE: While config is being reloaded it will use the old config in parsing if
            // the singleton is used.
            if (!loadConfig())
            {
                Console.WriteLine("New config failed to load, keeping old config...");
                return false;
            }
            Console.WriteLine("New config loaded, unloading old config...");
            // At this point configStaging loaded
            // only unload the existing config if it was valid (aka has chains)
            if (config.getChains().Count != 0)
            {
                // ignore if unload fails, as we always want to load.
                config.unload();
            }
            config = configStaging;
            return config.load();
        }
        public bool unload()
        {
            bool result = true;
            foreach (IModule module in config.getModules())
            {
                result = result && module.unload();
            }
            foreach (ILoadable daemon in config.getDaemons())
            {
                daemon.unload();
            }
            result = result && config.unload();
            return result;
        }
        public static string format(Context context)
        {
            return format(context.getCurrent(), context);
        }
        public static string format(string format, Context context)
        {
            if (format == null)
            {
                return "";
            }
            foreach (var singleContextKey in context.Keys)
            {
                if (singleContextKey != null & context[singleContextKey] != null)
                {
                    format = format.Replace("%" + singleContextKey + "%", context[singleContextKey]);
                }
            }
            return format;
        }
        public static Context run(IItem item, Context context, bool async)
        {
            Chain chain = ChatDirector.getConfig().getChainForItem(item);
            if (chain != null)
            {
                if (async)
                {
                    chain.runAsync(item, context);
                    return new Context();
                }
                else
                {
                    return chain.runAt(item, context);
                }
            }
            else
            {
                Console.WriteLine("Could not find chain to go with " + item);
                return new Context().halt();
            }
        }
        public static void addItem(IItem item, Chain chain)
        {
            ChatDirector.getConfig().putChainForItem(item, chain);
        }
        public static bool hasChains()
        {
            return getConfig().getChains().Count != 0;
        }
        public Type getItemClass(string itemType)
        {
            return config.getItemClass(itemType);
        }
        public Dictionary<string, Chain> getChains()
        {
            return config.getChains();
        }
        public Type getItemClass(string itemType, IEnumerable<IModule> modules)
        {
            return config.getItemClass(itemType, modules);
        }
        public static Configuration getConfig()
        {
            return instance.config;
        }
        public static Configuration getConfigStaging()
        {
            return instance.configStaging;
        }
        public static void setConfigStaging(Configuration config)
        {
            instance.configStaging = config;
        }
        public static ChatDirector getInstance()
        {
            return instance;
        }
        public IModule getModule(Type class1)
        {
            return config.getModule(class1);
        }
        public ILoadable getOrCreateDaemon(Type class1)
        {
            return config.getOrCreateDaemon(class1);
        }
        public bool isDebug()
        {
            return getConfig().isDebug();
        }
        public bool isTesting()
        {
            return getConfig().isTesting();
        }
        public bool isValid()
        {
            return getConfig().isValid();
        }
    }
}