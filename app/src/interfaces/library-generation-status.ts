export interface LibraryGenerationStatus {
    state: string;
    isProcessing: boolean;
    lastGeneratedDate: Date;
    movieCountTotal: number;
    movieCountCompleted: number;
    tvShowCountCompleted: number;
    tvShowCountTotal: number;
    countTotal: number;
    countCompleted: number;
    countRemaining: number;
    secondsRemaining: number;
    activeMovies: string[];
}