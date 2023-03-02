import { IConfig } from './config.interface';
import { globalConfig } from './config.global';

export const config: IConfig = {
  ...globalConfig,

  apiUrl: 'http://a-production-url',
};
