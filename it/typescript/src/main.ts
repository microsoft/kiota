import { App } from './app/app';
import { Logger } from './app/common/logger';

Logger.logTask('SYSTEM', 'STARTING');

App.run();

Logger.logTask('SYSTEM', 'FINISHED');
