import { bootstrapApplication } from '@angular/platform-browser';
import { AppServerConfig } from './app/app.config.server';
import { App } from './app/app';

const bootstrap = () => bootstrapApplication(App, AppServerConfig);
export default bootstrap;
