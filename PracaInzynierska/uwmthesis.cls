%% Copyright 2013 by Aleksadner Denisiuk <denisiuk@matman.uwm.edu.pl>
%% 
%% 
%% This work may be distributed and/or modified under the
%% conditions of the LaTeX Project Public License, either version 1.2
%% of this license or (at your option) any later version.
%% The latest version of this license is in
%%    http://www.latex-project.org/lppl.txt
%% and version 1.2 or later is part of all distributions of LaTeX
%% version 1999/12/01 or later.
%% 

\NeedsTeXFormat{LaTeX2e}
\ProvidesClass{uwmthesis}
\DeclareOption{wmii}{
  \newcommand{\wydzi@l}{Wydzia{\l} Matematyki i Informatyki}
  \newcommand{\ewydzi@l}{Faculty of Mathematics and Computer Science}
}
\DeclareOption{mat}{
  \newcommand{\kierun@k}{Matematyka}
  \newcommand{\ekierun@k}{Mathematics}
}
\DeclareOption{inf}{
  \newcommand{\kierun@k}{Informatyka}
  \newcommand{\ekierun@k}{Computer Science}
}
\DeclareOption{fiz}{
  \newcommand{\kierun@k}{Fizyka techniczna}
  \newcommand{\ekierun@k}{Technical Physics}
}
\DeclareOption{mgr}{
  \newcommand{\prac@}{magisterska}
  \newcommand{\eprac@}{Master's}
}
\DeclareOption{inz}{
  \newcommand{\prac@}{in\.zynierska}
  \newcommand{\eprac@}{Engineer's}
}
\DeclareOption{lic}{
  \newcommand{\prac@}{licencjacka}
  \newcommand{\eprac@}{Bachelor's}
}
\DeclareOption*{%
%  wszystkie opcje
\PassOptionsToClass{\CurrentOption}{mwbk}} % maj� by� przekazywane
% do klasy mwbk
\ProcessOptions
\LoadClass[12pt, oneside, wide]{mwbk} % �adujemy klas� bazow�
\ExecuteOptions{wmii, inf, mgr}
%%% binding offset
\newlength\@binding@ffset\setlength\@binding@ffset{8mm}
  \addtolength\oddsidemargin{\@binding@ffset}
  \addtolength\evensidemargin{-\@binding@ffset}
%%%
\newcommand\r@gged@enter{\leftskip0pt plus.4\hsize\rightskip\leftskip\parfillskip0pt \parindent 0 pt \spaceskip.3333em \xspaceskip.5em \pretolerance9999\tolerance9999\exhyphenpenalty9999\interlinepenalty9999\hyphenpenalty9999\let\\=\break}

\newenvironment{streszczenie}{\chapter*{Streszczenie}}{\relax}
\newenvironment{abstract}{\begingroup\chapter*{Abstract}\let\mw@markandtoc=\relax\section*{\@etitle}}{\global\let\@etitle\@empty\endgroup\relax}
\renewcommand\maketitle{\begin{titlepage}%
  \let\footnotesize\small
  \let\footnoterule\relax
  \let \footnote \thanks
  \begin{center}
  \Large
    \hbox to \hsize{\textsc{\hfil Uniwersytet Warmi\'nsko-Mazurski w~Olsztynie\hfil\r@gged@enter}}
    \hbox to \hsize{\hfil\textsc{\wydzi@l\r@gged@enter}\hfil}
  \end{center}
  \vskip 0pt plus 0.25 fill
  \begin{center}
  \large
\textbf{Kierunek: \kierun@k}
  \end{center}
  \vfill
  \begin{center}%
  \titlesettings
      \begin{tabular}[t]{c}%
        \Large\strut\bfseries\@author
      \end{tabular}\par%
        \vskip9ex
    {\LARGE\bfseries \@title \par}
        \vskip6ex
  \end{center}\par
  \vfill
  \begingroup%
    \rightskip0pt plus.124\hsize\parfillskip0pt \parindent 0 pt \spaceskip.3333em \xspaceskip.5em \pretolerance9999\tolerance9999\exhyphenpenalty9999\interlinepenalty9999\hyphenpenalty9999\let\\=\break
    \normalsize \parindent0pt Praca \prac@\ wykonana\leftskip0.35\hsize\par w~\@wykonanaw\hangindent2em\hangafter1\par
      \smallskip pod kierunkiem \par{\frenchspacing\@podkierunkiem}\hangindent2em\hangafter1\par
  \endgroup
  \vfill
  {\centering\large \@date, Olsztyn\par}\eject
\if@twoside
\hbox{ }\thispagestyle{empty}\vfill\eject
\fi
%% English
  \begin{center}
  \Large
    \hbox to \hsize{\textsc{\hfil University of Warmia and Mazury in Olsztyn\hfil\r@gged@enter}}
    \hbox to \hsize{\hfil\textsc{\ewydzi@l\r@gged@enter}\hfil}
  \end{center}
  \vskip 0pt plus 0.25 fill
  \begin{center}
  \large
\textbf{\ekierun@k}
  \end{center}
  \vfill
  \begin{center}%
  \titlesettings
      \begin{tabular}[t]{c}%
        \Large\strut\bfseries\@author
      \end{tabular}\par%
        \vskip9ex
    {\LARGE\bfseries \@etitle \par}
        \vskip6ex
  \end{center}\par
  \vfill
  \begingroup%
    \rightskip0pt plus.124\hsize\parfillskip0pt \parindent 0 pt \spaceskip.3333em \xspaceskip.5em \pretolerance9999\tolerance9999\exhyphenpenalty9999\interlinepenalty9999\hyphenpenalty9999\let\\=\break
    \normalsize \parindent0pt \eprac@\ Thesis is performed \leftskip0.35\hsize\par in~\@ewykonanaw\hangindent2em\hangafter1\par
      \smallskip under supervision of \par{\frenchspacing\@epodkierunkiem}\hangindent2em\hangafter1\par
  \endgroup
  \vfill\thispagestyle{empty}
  {\centering\large \@date, Olsztyn\par}\eject
\if@twoside
\hbox{ }\thispagestyle{empty}\vfill\eject
\fi
%% End English
  \end{titlepage}%
  \normalsfcodes
  \setcounter{footnote}{0}%
  \setcounter{page}{1}%
  \global\let\thanks\relax
  \global\let\maketitle\relax
  \global\let\@thanks\@empty
  \global\let\@author\@empty
  \global\let\@date\@empty
  \global\let\@title\@empty
  \global\let\@wykonanaw\@empty
  \global\let\@podkierunkiem\@empty
  \global\let\podkierunkiem\relax
  \global\let\wykonanaw\relax
  \global\let\etitle\relax
  \global\let\title\relax
  \global\let\author\relax
  \global\let\date\relax
  \global\let\and\relax
}

\newcommand\etitle[1]{\renewcommand\@etitle{#1}}
\newcommand\@etitle{\ClassError{uwmthesis}{No \noexpand\etitle given}\@ehc}

\newcommand\bibsep[1]{\par\medbreak{\bfseries#1}\par\nobreak}

\newcommand\wykonanaw[1]{\renewcommand\@wykonanaw{#1}}
\newcommand\@wykonanaw{\ClassError{uwmthesis}{No \noexpand\wykonanaw given}\@ehc}

\newcommand\ewykonanaw[1]{\renewcommand\@ewykonanaw{#1}}
\newcommand\@ewykonanaw{\ClassError{uwmthesis}{No \noexpand\ewykonanaw given}\@ehc}

\newcommand\podkierunkiem[1]{\renewcommand\@podkierunkiem{#1}}
\newcommand\@podkierunkiem{\ClassError{uwmthesis}{No \noexpand\podkierunkiem given}\@ehc}

\newcommand\epodkierunkiem[1]{\renewcommand\@epodkierunkiem{#1}}
\newcommand\@epodkierunkiem{\ClassError{uwmthesis}{No \noexpand\epodkierunkiem given}\@ehc}

\pagestyle{plain}
\endinput
