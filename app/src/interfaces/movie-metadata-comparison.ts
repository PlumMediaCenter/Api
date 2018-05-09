export interface MovieMetadataComparison {
    incoming: MovieMetadata;
    current: MovieMetadata;
}
export interface MovieMetadata {
    posterUrls: any[];
    backdropUrls: any[];
    title: string;
    titles: string[];
    summary: string;
    description: string;
    collection?: any;
    collectionOrder?: any;
    cast: Cast[];
    crew: Crew[];
    genres: string[];
    keywords: string[];
    rating: string;
    releaseDate: Date;
    runtime: number;
    tmdbId: number;
}

export interface Cast {
    tmdbId: number;
    character: string;
    name: string;
}

export interface Crew {
    tmdbId: number;
    job: string;
    name: string;
}
