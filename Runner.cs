﻿using System;
using System.Collections.Generic;

namespace SupportTool
{
    class Runner
    {
        private Config config;
        private FileAggregator fileAggregator;
        private LoggerInterface logger;

        public Runner(Config config, FileAggregator fileAggregator, LoggerInterface logger)
        {
            this.config = config;
            this.fileAggregator = fileAggregator;
            this.logger = logger;
        }

        public void Run(List<CommandInterface> commands)
        {
            Propagation propagation = new Propagation();

            foreach (CommandInterface command in commands)
            {
                if (propagation.ShouldStop)
                {
                    logger.Log("Aborted");
                    return;
                }

                if (command is CommandCheckBoxInterface)
                {
                    CommandCheckBoxInterface excludable = (CommandCheckBoxInterface)command;

                    bool shouldRun = (bool)config.GetType().GetProperty(excludable.ConfigPropertyPath).GetValue(config);

                    if (!shouldRun)
                    {
                        logger.Log("Skipping "+excludable.Text);
                        continue;
                    }
                }

                try
                {
                    command.Execute(config, fileAggregator, logger, propagation);
                }
                catch (Exception e)
                {
                    logger.Log(e.ToString());
#if DEBUG
                    throw e;
#endif
                }
            }
        }
    }
}
