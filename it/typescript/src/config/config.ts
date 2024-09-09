import { IConfig } from './config.interface';
import { globalConfig } from './config.global';

export const config: IConfig = {
  ...globalConfig,

  apiUrl: 'https://a-production-url',
};
