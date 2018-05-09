import { Config } from '../config/local';
//import { Config } from '../config/pi3';

import { BrowserModule } from '@angular/platform-browser';
import { ErrorHandler, NgModule } from '@angular/core';
import { IonicApp, IonicErrorHandler, IonicModule } from 'ionic-angular';

import { MyApp } from './app.component';
import { HomePage } from '../pages/home/home';

import { StatusBar } from '@ionic-native/status-bar';
import { SplashScreen } from '@ionic-native/splash-screen';

import { MovieCardComponent } from '../components/movie-card/movie-card';
import { Api } from '../providers/api';
import { Http2Factory } from '../providers/http2-factory';
import { HttpModule } from '@angular/http';
import { MovieInfoClick } from '../directives/movie-info-click';
import { MovieInfoPage } from '../pages/movie-info/movie-info';
import { MovieMetadataPage } from '../pages/movie-metadata/movie-metadata';
import { MovieMetadataClick } from '../directives/movie-metadata-click';
import { MetadataCompareRowComponent } from '../components/metadata-compare-row/metadata-compare-row';
import { ImageListComponent } from '../components/image-list/image-list';
import { ProperCaseSpacePipe } from '../pipes/proper-case-space-pipe';
import { StringListComponent } from '../components/string-list/string-list';
import { AdminPage } from '../pages/admin/admin';
import { MoviePlayPage } from '../pages/movie-play/movie-play';
import { MoviePlayClick } from '../directives/movie-play-click';
import { VideojsVideoComponent } from '../components/videojs-video/videojs-video';
import { ImageSwapperDirective } from '../directives/image-swapper';
import { GoToPageClickDirective } from '../directives/go-to-page-click';
import { SourcesPage } from '../pages/sources/sources';
import { SaveIconComponent } from '../components/save-icon/save-icon';
import { Alerter } from '../providers/alerter';
import { Loader } from '../providers/loader';
import { Toaster } from '../providers/toaster';
import { Config as BaseConfig } from '../config/config';
import { InstallDatabasePage } from '../pages/install-database/install-database';
import { InitializePage } from '../pages/initialize/initialize';
import { Util } from '../providers/util';
import { ProgressBarComponent } from '../components/progress-bar/progress-bar';
import { FormsModule } from '@angular/forms';
import { MediaItemProcessClick } from '../directives/media-item-process-click';
import { AccountPage } from '../pages/account/account';
import { HistoryPage } from '../pages/history/history';
import { ProgressSparseComponent } from '../components/progress-sparse/progress-sparse';
import { DeviceSizeIfDirective } from '../directives/device-size-if';
import { MediaInfoClick } from '../directives/media-info-click';
import { SearchResultsPage } from '../pages/search-results/search-results';
import { SearchInputComponent } from '../components/search-input/search-input';

@NgModule({
    declarations: [
        MyApp,

        //pages
        AdminPage,
        AccountPage,
        HomePage,
        HistoryPage,
        MovieInfoPage,
        MovieMetadataPage,
        MoviePlayPage,
        SearchResultsPage,
        SourcesPage,
        InstallDatabasePage,
        InitializePage,

        //components
        MovieCardComponent,
        MetadataCompareRowComponent,
        ImageListComponent,
        ProgressSparseComponent,
        StringListComponent,
        VideojsVideoComponent,
        SaveIconComponent,
        SearchInputComponent,
        ProgressBarComponent,

        //directives
        DeviceSizeIfDirective,
        MovieInfoClick,
        MediaInfoClick,
        MovieMetadataClick,
        MoviePlayClick,
        ImageSwapperDirective,
        GoToPageClickDirective,
        MediaItemProcessClick,

        //pipes
        ProperCaseSpacePipe
    ],
    imports: [
        BrowserModule,
        FormsModule,
        HttpModule,
        IonicModule.forRoot(MyApp),
    ],
    bootstrap: [IonicApp],
    entryComponents: [
        MyApp,
        HomePage,
        AdminPage,
        AccountPage,
        HistoryPage,
        MovieInfoPage,
        MovieMetadataPage,
        MoviePlayPage,
        SearchResultsPage,
        SourcesPage,
        InstallDatabasePage,
        InitializePage
    ],
    providers: [
        Api,
        Http2Factory,
        StatusBar,
        SplashScreen,
        Alerter,
        Loader,
        Toaster,
        Util,
        { provide: ErrorHandler, useClass: IonicErrorHandler },
        { provide: BaseConfig, useClass: Config }
    ]
})
export class AppModule { }
