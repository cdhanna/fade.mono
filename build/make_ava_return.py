"""The words of the way in to the long visit, of its last question, and of what comes after she has gone.

This is the one place that these lines are written down, in every language. It adds any row that angler.csv,
menu.csv or ability.csv does not have yet, and it changes nothing that is there: a row that the sheet already has keeps the sheet's
words, because a sync puts them back. A reworded line needs a new key. See fish_headers_ava.

    py -I build/make_ava_return.py

Rules for every line: she never says "free" or "love". She never guilts the player for going. Every rule of the game
that she states is true, the hints most of all. She is FEMALE, and nothing may give away the player's gender, which
takes care in the past tense in French, Polish and Portuguese.
"""

import csv
import io
import pathlib

LANG = pathlib.Path(__file__).resolve().parent.parent / "Fade.MonoGame" / "Assets" / "Fish" / "Lang"

AVA = ("Ava, the anglerfish who keeps the shop. She is FEMALE: use feminine forms for her. Never reveal the player's "
       "gender. Dry and calm, secretly fond. Never say 'free' or 'love'. ")
BOX = AVA + "Said in the angler's box, a page of four lines at most (about 200 letters)."
REPLY = "A reply the silent player can pick when talking to Ava, one line in her box. Very short. No gender."
WATER = (AVA + "The secret ending where the player stays with her. The words float over the puzzle board, so keep it "
         "short (about 60 letters).")
LETTER = (AVA + "A line of the letter in a bottle that she sends after the player has left her. Shown alone, in big "
          "letters across the main menu: about 90 letters at most. Flat and plain, as if she is fine.")

# key: (en, pt-BR, fr, de, pl), note
ANGLER = {}


def add(key, words, note):
    assert key not in ANGLER, key
    assert len(words) == 5, key
    ANGLER[key] = (words, note)


# THE WAY IN. the first time she is asked anything after an ending, she says two things about it.
WELCOME = AVA + ("The first thing she says when asked a question after the player reached an ending of the game. "
                 "A page in her box, four lines at most.")
add("angler.ava.welcome.home.a", (
    "You finished a whole dive. Every round of it. The fish went home.",
    "Você terminou um mergulho inteiro. Todas as rodadas. Os peixes voltaram para casa.",
    "Tu as fini une plongée entière. Chaque manche. Les poissons sont rentrés chez eux.",
    "Du hast einen ganzen Tauchgang geschafft. Jede Runde. Die Fische sind nach Hause geschwommen.",
    "Całe nurkowanie, do ostatniej rundy. Ryby wróciły do domu."), WELCOME)
add("angler.ava.welcome.home.b", (
    "Well done. I mean that. And now you are back down here. Hm.",
    "Muito bem. É sério. E agora você está de volta aqui embaixo. Hum.",
    "Bravo. Je le pense. Et te revoilà ici, en bas. Hm.",
    "Gut gemacht. Ehrlich. Und jetzt bist du wieder hier unten. Hm.",
    "Brawo. Naprawdę. A teraz znowu tu jesteś, na dole. Hm."), WELCOME)
add("angler.ava.welcome.reef.a", (
    "You grew the reef. All of that coral. I could hear it from here.",
    "Você fez o recife crescer. Todo aquele coral. Deu para ouvir daqui.",
    "Tu as fait pousser le récif. Tout ce corail. Je l'entendais d'ici.",
    "Du hast das Riff wachsen lassen. All die Korallen. Ich habe es bis hierher gehört.",
    "Rafa urosła. Cały ten koral. Słyszałam to aż tutaj."), WELCOME)
add("angler.ava.welcome.reef.b", (
    "Well done. And you came back down anyway. Interesting.",
    "Muito bem. E mesmo assim você voltou aqui para baixo. Interessante.",
    "Bravo. Et te revoilà en bas quand même. Intéressant.",
    "Gut gemacht. Und trotzdem bist du wieder heruntergekommen. Interessant.",
    "Brawo. A mimo to znowu tu jesteś. Ciekawe."), WELCOME)
add("angler.ava.welcome.ancient.a", (
    "You woke it. The old one. The whole sea felt that.",
    "Você o acordou. O antigo. O mar inteiro sentiu.",
    "Tu l'as réveillé. L'ancien. Toute la mer l'a senti.",
    "Du hast es geweckt. Das Alte. Das ganze Meer hat es gespürt.",
    "Obudziło się. To stare. Całe morze to poczuło."),
    WELCOME + " This ending is the horror one, where something ancient wakes. She is shaken, and does NOT congratulate.")
add("angler.ava.welcome.ancient.b", (
    "And then you came back down to ask me about fish. I am not cross. I am a little cross.",
    "E depois você voltou aqui para me perguntar sobre peixes. Não estou brava. Estou um pouco brava.",
    "Et ensuite, te voilà qui reviens me parler de poissons. Je ne suis pas fâchée. Un peu fâchée.",
    "Und dann kommst du wieder runter und fragst mich nach Fischen. Ich bin nicht böse. Ein bisschen böse.",
    "A potem znowu tu jesteś i pytasz mnie o ryby. Nie gniewam się. Trochę się gniewam."),
    WELCOME + " This ending is the horror one, where something ancient wakes. She is shaken, and does NOT congratulate.")
add("angler.ava.welcome.diver.a", (
    "The diver went back up with a full album. A picture of every fish.",
    "O mergulhador subiu com o álbum completo. Uma foto de cada peixe.",
    "Le plongeur est remonté avec un album complet. Une photo de chaque poisson.",
    "Der Taucher ist mit vollem Album aufgetaucht. Ein Bild von jedem Fisch.",
    "Nurek wypłynął z pełnym albumem. Zdjęcie każdej ryby."), WELCOME)
add("angler.ava.welcome.diver.b", (
    "Nobody comes back down after something like that. You did.",
    "Ninguém volta aqui para baixo depois de uma coisa dessas. Você voltou.",
    "Personne ne redescend après un truc pareil. Toi, si.",
    "Nach so etwas kommt niemand wieder herunter. Du schon.",
    "Po czymś takim nikt tu nie wraca. A ty tak."), WELCOME)
add("angler.ava.welcome.neptune.a", (
    "The old god of the sea favored you. Nobody gets that.",
    "O velho deus do mar te favoreceu. Ninguém consegue isso.",
    "Le vieux dieu de la mer t'a accordé sa faveur. Ça n'arrive à personne.",
    "Der alte Gott des Meeres war dir gewogen. Das bekommt sonst niemand.",
    "Stary bóg morza okazał ci łaskę. Nikt tego nie dostaje."), WELCOME)
add("angler.ava.welcome.neptune.b", (
    "And still you are here, with me and the shrimp. Well done, I suppose.",
    "E mesmo assim você está aqui, comigo e com os camarões. Muito bem, eu acho.",
    "Et pourtant te voilà, avec moi et les crevettes. Bravo, j'imagine.",
    "Und trotzdem bist du hier, bei mir und den Garnelen. Gut gemacht, schätze ich.",
    "A jednak tu jesteś, ze mną i krewetkami. Brawo, chyba."), WELCOME)

# the sea that is left to see: once an ending has been reached, she has a hint at one that has not.
HINT = AVA + ("Her answer to 'Is there more down here?': a TRUE, slightly vague hint at an ending of the game that the "
              "player has not reached. A page in her box, four lines at most.")
add("angler.ava.sea.ask", (
    "Is there more down here?",
    "Tem mais coisa aqui embaixo?",
    "Il y a autre chose, en bas ?",
    "Gibt es hier unten noch mehr?",
    "Jest tu na dole coś jeszcze?"),
    "A question the player picks, one line in the angler's box: keep it short (about 40 letters).")
add("angler.ava.sea.home", (
    "There is the plain ending. Finish every round of a dive and see where the fish go. Nobody minds a plain ending.",
    "Tem o final simples. Termine todas as rodadas de um mergulho e veja para onde os peixes vão. Ninguém se importa com um final simples.",
    "Il y a la fin toute simple. Termine chaque manche d'une plongée et regarde où vont les poissons. Personne ne méprise une fin simple.",
    "Es gibt das schlichte Ende. Schaff jede Runde eines Tauchgangs und sieh, wohin die Fische gehen. Ein schlichtes Ende ist nichts Schlimmes.",
    "Jest zwykłe zakończenie. Przejdź każdą rundę nurkowania i zobacz, dokąd pójdą ryby. Zwykłe zakończenie nikomu nie przeszkadza."), HINT)
add("angler.ava.sea.reef", (
    "The coral. Grow enough of the good kind to cover more than half the board, and the sea does something about it.",
    "O coral. Faça crescer tanto do tipo bom que ele cubra mais da metade do tabuleiro, e o mar vai reagir.",
    "Le corail. Fais pousser assez du bon pour couvrir plus de la moitié du plateau, et la mer réagira.",
    "Die Korallen. Lass so viele von den guten wachsen, dass sie mehr als das halbe Brett bedecken, und das Meer reagiert darauf.",
    "Koral. Wyhoduj tyle dobrego, żeby pokrył ponad połowę planszy, a morze coś z tym zrobi."), HINT)
add("angler.ava.sea.ancient", (
    "There is a sick coral, green and wrong. Let it cover more than half the board and something old wakes up. I would not. You might.",
    "Tem um coral doente, verde e errado. Deixe-o cobrir mais da metade do tabuleiro e algo antigo acorda. Eu não faria isso. Você talvez faça.",
    "Il y a un corail malade, vert et pas normal. Laisse-le couvrir plus de la moitié du plateau, et quelque chose d'ancien se réveille. Moi, je ne le ferais pas. Toi, peut-être.",
    "Es gibt eine kranke Koralle, grün und falsch. Lass sie mehr als das halbe Brett bedecken, und etwas Altes wacht auf. Ich würde es nicht tun. Du vielleicht.",
    "Jest chory koral, zielony i nie taki, jak trzeba. Pozwól mu pokryć ponad połowę planszy, a obudzi się coś starego. Ja bym nie próbowała. Ty może tak."), HINT)
add("angler.ava.sea.diver", (
    "Carry a bottle up to the top row instead of down, and a diver comes with a camera. They want a picture of every fish.",
    "Leve uma garrafa até a fileira de cima, em vez de para baixo, e um mergulhador aparece com uma câmera. Ele quer uma foto de cada peixe.",
    "Monte une bouteille jusqu'à la rangée du haut au lieu de la faire couler, et un plongeur arrive avec un appareil photo. Il veut une photo de chaque poisson.",
    "Bring eine Flasche nach oben in die oberste Reihe statt nach unten, und ein Taucher kommt mit einer Kamera. Er will ein Bild von jedem Fisch.",
    "Wynieś butelkę do górnego rzędu zamiast na dół, a przypłynie nurek z aparatem. Chce mieć zdjęcie każdej ryby."), HINT)
add("angler.ava.sea.neptune", (
    "At night, the ink of the squid makes shapes. Make the right five, one a night, and someone very old takes an interest.",
    "À noite, a tinta da lula forma desenhos. Faça os cinco certos, um por noite, e alguém muito antigo se interessa.",
    "La nuit, l'encre du calmar dessine des formes. Fais les cinq bonnes, une par nuit, et quelqu'un de très ancien s'y intéresse.",
    "Nachts formt die Tinte des Tintenfischs Gestalten. Mach die richtigen fünf, eine pro Nacht, und jemand sehr Altes wird aufmerksam.",
    "Nocą atrament kałamarnicy układa się w kształty. Ułóż właściwe pięć, po jednym na noc, a ktoś bardzo stary się zainteresuje."),
    HINT + " The shapes are painted in the squid's ink: never call them stars.")
add("angler.ava.sea.more", (
    "So there is still that. No need to go anywhere yet.",
    "Então ainda tem isso. Não precisa ir a lugar nenhum ainda.",
    "Donc il reste encore ça. Pas besoin de partir tout de suite.",
    "Also gibt es noch das. Kein Grund, schon irgendwohin zu gehen.",
    "Więc to jeszcze zostało. Nie ma potrzeby nigdzie się wybierać."),
    BOX + " Said after a hint at an ending the player has not seen: she would rather the player did not leave yet.")
add("angler.ava.sea.more2", (
    "So there is still that. Go and see it for yourself.",
    "Então ainda tem isso. Vá ver com seus próprios olhos.",
    "Il reste donc ça. Va le voir par toi-même.",
    "Das gibt es also noch. Sieh es dir selbst an.",
    "Więc jest jeszcze to. Idź i zobacz to na własne oczy."),
    BOX + " Said after a hint at an ending the player has not seen. She wants the player to go and have their own "
          "adventures, not to depend on her.")

# THE LAST QUESTION. every ending has been reached, and the player starts a new game.
OFFER = BOX + " Every ending of the game has been reached, and she asks if the player is leaving now."
add("angler.ava.offer.a", (
    "That is all of them. Every ending this sea has. You have seen the lot.",
    "São todos. Todos os finais que este mar tem. Você viu tudo.",
    "C'est tout. Toutes les fins que cette mer a. Tu as tout vu.",
    "Das waren alle. Jedes Ende, das dieses Meer hat. Du hast alles gesehen.",
    "To już wszystkie. Każde zakończenie tego morza. Wszystko już za tobą."), OFFER)
add("angler.ava.offer.b", (
    "So you will be going. You should, really. Go on.",
    "Então você vai embora. Deveria mesmo. Pode ir.",
    "Donc tu vas partir. Tu devrais, vraiment. Vas-y.",
    "Dann gehst du jetzt. Solltest du auch, wirklich. Na los.",
    "Więc pora iść. Naprawdę, tak trzeba. No idź."), OFFER)
add("angler.ava.reply.stay", ("I'm staying.", "Vou ficar.", "Je reste.", "Ich bleibe.", "Zostaję."), REPLY)
add("angler.ava.reply.go", ("I'm leaving.", "Vou embora.", "Je m'en vais.", "Ich gehe.", "Odchodzę."), REPLY)
add("angler.ava.reply.dive", ("One more dive.", "Mais um mergulho.", "Encore une plongée.", "Noch ein Tauchgang.",
                              "Jeszcze jedno nurkowanie."), REPLY)
add("angler.ava.offer.stay", (
    "Oh. Then come down. Further than this.",
    "Ah. Então desça. Mais fundo que isto.",
    "Oh. Alors descends. Plus bas que ça.",
    "Oh. Dann komm runter. Tiefer als hier.",
    "Och. To schodź. Głębiej niż tu."), OFFER)
add("angler.ava.offer.go", (
    "Go on, friend.",
    "Vai, amigo.",
    "Va, l'ami.",
    "Geh nur, mein Freund.",
    "Idź, przyjacielu."), OFFER + " Her last words as the player leaves her for good.")
add("angler.ava.offer.dive", (
    "Of course. One more. I will be at the shop.",
    "Claro. Mais um. Estarei na loja.",
    "Bien sûr. Encore une. Je serai à la boutique.",
    "Natürlich. Noch einer. Ich bin im Laden.",
    "Jasne. Jeszcze jedno. Będę w sklepie."), OFFER)
add("angler.ava.offer.dive2", (
    "Ask me, when you know. I can wait.",
    "Me pergunte quando souber. Eu posso esperar.",
    "Demande-moi quand tu sauras. Je peux attendre.",
    "Frag mich, wenn du es weißt. Ich kann warten.",
    "Zapytaj, kiedy będziesz wiedzieć. Mogę poczekać."), OFFER)

# the same question, later, from her list of questions, for a player who said "One more dive"
LATER = BOX + " The player said 'One more dive' when she asked if they were leaving, and now brings it up again."
add("angler.ava.leave.ask", (
    "About your question...",
    "Sobre a sua pergunta...",
    "À propos de ta question...",
    "Wegen deiner Frage...",
    "Co do twojego pytania..."),
    "A question the player picks, one line in the angler's box: keep it short (about 40 letters). The player brings "
    "back her question 'are you leaving?'.")
add("angler.ava.leave.a", (
    "Have you decided, then? Are you leaving?",
    "Já decidiu, então? Vai embora?",
    "Alors, c'est décidé ? Tu t'en vas ?",
    "Hast du dich entschieden? Gehst du?",
    "No więc? Odchodzisz?"), LATER)
add("angler.ava.reply.wait", ("Not yet.", "Ainda não.", "Pas encore.", "Noch nicht.", "Jeszcze nie."), REPLY)
add("angler.ava.leave.stay", (
    "Then finish this dive. When you start the next one, keep going down.",
    "Então termine este mergulho. Quando começar o próximo, continue descendo.",
    "Alors finis cette plongée. Quand tu commenceras la prochaine, continue de descendre.",
    "Dann bring diesen Tauchgang zu Ende. Wenn du den nächsten beginnst, tauch einfach weiter.",
    "To skończ to nurkowanie. Kiedy zaczniesz następne, płyń dalej w dół."), LATER)
add("angler.ava.leave.wait", (
    "Mm. Not yet, then.",
    "Hum. Ainda não, então.",
    "Mm. Pas encore, alors.",
    "Mm. Also noch nicht.",
    "Mm. Czyli jeszcze nie."), LATER)

# AFTER SHE HAS GONE: the letter in the bottle
add("angler.ava.bottle.l1", ("Friend.", "Amigo.", "L'ami.", "Mein Freund.", "Przyjacielu."), LETTER)
add("angler.ava.bottle.l2", (
    "It is quiet down here. I made a board. Nobody popped it, so it is still very tidy.",
    "Está quieto aqui embaixo. Fiz um tabuleiro. Ninguém estourou nada, então continua bem arrumado.",
    "C'est calme, en bas. J'ai fait un plateau. Personne n'y a touché, alors il est toujours bien rangé.",
    "Hier unten ist es ruhig. Ich habe ein Brett gemacht. Niemand hat etwas platzen lassen, also ist es noch sehr ordentlich.",
    "Tu na dole jest cicho. Zrobiłam planszę. Nikt jej nie ruszył, więc wciąż jest bardzo schludna."), LETTER)
add("angler.ava.bottle.l3", (
    "Gary sends his regards. He does not, but he would.",
    "O Gary manda lembranças. Não manda, mas mandaria.",
    "Gary te salue. Ce n'est pas vrai, mais il le ferait.",
    "Gary lässt grüßen. Tut er nicht, aber er würde.",
    "Gary przesyła pozdrowienia. Nie przesyła, ale by przesłał."),
    LETTER + " Gary is one of her exes, now a lump on her side who no longer talks.")
add("angler.ava.bottle.l4", (
    "This is the word I said I would send. Do not read anything into it.",
    "Estas são as notícias que eu disse que mandaria. Não tire conclusões.",
    "Voilà les nouvelles que j'avais promis d'envoyer. N'y vois rien de particulier.",
    "Das ist die Nachricht, die ich versprochen habe. Interpretier nichts hinein.",
    "To ta wiadomość, którą obiecałam wysłać. Nie doszukuj się w niej niczego."), LETTER)
add("angler.ava.bottle.l5", (
    "If you want to answer, the radio still works. {$code}",
    "Se quiser responder, o rádio ainda funciona. {$code}",
    "Si tu veux répondre, la radio marche toujours. {$code}",
    "Wenn du antworten willst: Das Funkgerät geht noch. {$code}",
    "Jeśli zechcesz odpowiedzieć, radio wciąż działa. {$code}"),
    LETTER + " {$code} is the word FRIEND in Morse code, dots and dashes. Keep it at the end.")
add("angler.ava.bottle.l6", ("Ava.", "Ava.", "Ava.", "Ava.", "Ava."), LETTER + " Her name, signing the letter.")
add("angler.ava.bottle.hint", ("{confirm} go on", "{confirm} continuar", "{confirm} continuer", "{confirm} weiter",
                               "{confirm} dalej"), "The button hint along the bottom while the letter is read. Very short.")

# and when the player taps her name on the radio
BACK = BOX + " The player has left her, read her letter, and tapped FRIEND in Morse code on the radio to call her back."
add("angler.ava.back.a2", (
    "Friend. You tapped it badly. But I knew it was you.",
    "Amigo. Você bateu mal. Mas eu sabia que era você.",
    "L'ami. Tu as mal tapé. Mais je savais que c'était toi.",
    "Mein Freund. Schlecht gefunkt. Aber ich wusste, dass du es bist.",
    "Przyjacielu. Wystukane kiepsko. Ale wiedziałam, że to ty."),
    BACK + " The word the player tapped is FRIEND, what she always calls them.")
add("angler.ava.back.b", (
    "I said not to wait. I never said not to come back.",
    "Eu disse para não esperar. Nunca disse para não voltar.",
    "J'ai dit de ne pas attendre. Je n'ai jamais dit de ne pas revenir.",
    "Ich sagte, warte nicht. Ich sagte nie, komm nicht zurück.",
    "Mówiłam, żeby nie czekać. Nigdy nie mówiłam, żeby nie wracać."), BACK)
add("angler.ava.back.again", ("You again. Good.", "Você de novo. Que bom.", "Encore toi. Bien.", "Du schon wieder. Gut.",
                              "Znowu ty. Dobrze."), BACK + " She has been called back like this before.")
add("angler.ava.back.ask", ("Are you coming down?", "Vai descer?", "Tu descends ?", "Kommst du runter?",
                            "Schodzisz na dół?"), BACK)
add("angler.ava.reply.notnow", ("Not now.", "Agora não.", "Pas maintenant.", "Jetzt nicht.", "Nie teraz."), REPLY)
add("angler.ava.back.stay", (
    "Then come. Your place is where I kept it.",
    "Então venha. Seu lugar está onde eu guardei.",
    "Alors viens. Ta place est là où je l'ai gardée.",
    "Dann komm. Dein Platz ist da, wo ich ihn freigehalten habe.",
    "To chodź. Twoje miejsce czeka tam, gdzie je zachowałam."), BACK)
add("angler.ava.back.later", (
    "Mm. Tap again when you mean it.",
    "Hum. Bata de novo quando for pra valer.",
    "Mm. Tape encore quand ce sera pour de bon.",
    "Mm. Funk wieder, wenn du es ernst meinst.",
    "Mm. Zastukaj znowu, kiedy to będzie na serio."), BACK)

# THE BOARDS. what she says as the first gate and the first tent come in, and when the boards from ten on are clear.
add("angler.ava.gate.a", (
    "These two do not touch. You still cannot get between them.",
    "Esses dois não se tocam. Mesmo assim, não dá para passar entre eles.",
    "Ces deux-là ne se touchent pas. Tu ne peux pas passer entre eux quand même.",
    "Die zwei berühren sich nicht. Trotzdem kommst du nicht zwischen ihnen durch.",
    "Te dwie się nie stykają. A i tak nie przejdziesz między nimi."),
    WATER + " Two belly up fish stand corner to corner, and the cursor cannot pass between them.")
add("angler.ava.gate.b", (
    "To get past, one of them has to pop. Choose which, and when.",
    "Para passar, um deles tem que estourar. Escolha qual, e quando.",
    "Pour passer, il faut en faire éclater un. Choisis lequel, et quand.",
    "Um vorbeizukommen, muss einer platzen. Such dir aus, welcher und wann.",
    "Żeby przejść, jedna musi pęknąć. Wybierz którą i kiedy."),
    WATER + " A belly up fish pops when the cursor comes to it.")
add("angler.ava.tent.a", (
    "This one has a roof. Mind which side of it you are on.",
    "Este tem um telhado. Repare de que lado dele você está.",
    "Celui-ci a un toit. Fais attention de quel côté tu es.",
    "Das hier hat ein Dach. Achte darauf, auf welcher Seite du bist.",
    "Ta ma dach. Uważaj, po której stronie jesteś."),
    WATER + " Belly up fish stand in a tent shape on the sea floor, with fish under it.")
add("angler.ava.tent.b", (
    "Getting in or out costs one of them. Choose which.",
    "Entrar ou sair custa um deles. Escolha qual.",
    "Entrer ou sortir en coûte un. Choisis lequel.",
    "Rein oder raus kostet einen von ihnen. Such dir aus, welchen.",
    "Wejście albo wyjście kosztuje jedną z nich. Wybierz którą."), WATER)

CLEARED = WATER + " Said when the board is clear, which is the goal."
for board, words in (
    (10, ("Ten. I did not count. I just know.", "Dez. Não contei. Só sei.", "Dix. Je n'ai pas compté. Je le sais, c'est tout.",
          "Zehn. Ich habe nicht gezählt. Ich weiß es einfach.", "Dziesięć. Nie liczyłam. Po prostu wiem.")),
    (11, ("Even the belly up one. Tidy.", "Até o de barriga para cima. Arrumado.", "Même celui qui avait le ventre en l'air. Propre.",
          "Sogar der mit dem Bauch nach oben. Ordentlich.", "Nawet ta brzuchem do góry. Schludnie.")),
    (12, ("Clear. The water likes you, I think.", "Limpo. Acho que a água gosta de você.", "Propre. Je crois que l'eau t'aime bien.",
          "Leer. Ich glaube, das Wasser mag dich.", "Pusto. Chyba woda cię lubi.")),
    (13, ("There. I was only watching a little.", "Pronto. Eu só estava olhando um pouco.", "Voilà. Je regardais à peine.",
          "So. Ich habe nur ein bisschen zugesehen.", "No. Tylko trochę patrzyłam.")),
    (14, ("Past the gate. I saw which one you picked.", "Passou do portão. Vi qual você escolheu.", "La porte est passée. J'ai vu lequel tu as choisi.",
          "Am Tor vorbei. Ich habe gesehen, welchen du genommen hast.", "Za bramą. Widziałam, którą wybrano.")),
    (15, ("No rushing. Nobody rushes down here.", "Sem pressa. Ninguém tem pressa aqui embaixo.", "Sans te presser. Personne ne se presse, en bas.",
          "Ohne Hetze. Hier unten hetzt niemand.", "Bez pośpiechu. Tu na dole nikt się nie spieszy.")),
    (16, ("The bubble did not get far. Neither do I, mostly.", "A bolha não foi longe. Eu também não vou, quase nunca.",
          "La bulle n'est pas allée loin. Moi non plus, la plupart du temps.", "Die Blase ist nicht weit gekommen. Ich meistens auch nicht.",
          "Bańka daleko nie zaszła. Ja zwykle też nie.")),
    (17, ("However long that took, I did not mind.", "Quanto tempo levou, não me importei.", "Le temps que ça a pris, ça m'était égal.",
          "Wie lange es auch gedauert hat, mich hat es nicht gestört.", "Ile by to nie trwało, nie przeszkadzało mi.")),
    (18, ("Every one of them gone. Tidy.", "Todos eles se foram. Arrumado.", "Tous partis. Propre.", "Alle weg. Ordentlich.",
          "Wszystkie zniknęły. Schludnie.")),
    (19, ("You are still answering. In your way.", "Você continua respondendo. Do seu jeito.", "Tu réponds toujours. À ta façon.",
          "Du antwortest immer noch. Auf deine Art.", "Wciąż odpowiadasz. Po swojemu.")),
    (20, ("Nothing left. I keep making the nothing bigger.", "Não sobrou nada. Continuo fazendo o nada maior.",
          "Plus rien. Je fais le rien de plus en plus grand.", "Nichts mehr da. Ich mache das Nichts immer größer.",
          "Nic nie zostało. Ciągle robię to nic większym.")),
    (21, ("The rainbow ones went together. They like company too.", "Os de arco-íris foram juntos. Eles também gostam de companhia.",
          "Les arcs-en-ciel sont partis ensemble. Eux aussi aiment la compagnie.",
          "Die Regenbogenfische sind zusammen gegangen. Sie mögen auch Gesellschaft.", "Tęczowe odeszły razem. One też lubią towarzystwo.")),
    (22, ("I did not help. I wanted to.", "Eu não ajudei. Eu quis.", "Je n'ai pas aidé. J'en avais envie.", "Ich habe nicht geholfen. Ich wollte.",
          "Nie pomogłam. A chciałam.")),
    (23, ("Quiet again. I am getting used to quiet with you in it.", "Silêncio de novo. Estou me acostumando com o silêncio com você nele.",
          "Le calme, encore. Je m'habitue au calme avec toi dedans.", "Wieder still. Ich gewöhne mich an die Stille mit dir darin.",
          "Znowu cisza. Przyzwyczajam się do ciszy, w której jesteś.")),
    (24, ("Gary never finished one of these. He said the fish were looking at him.",
          "O Gary nunca terminou um destes. Dizia que os peixes ficavam olhando para ele.",
          "Gary n'en a jamais fini un. Il disait que les poissons le regardaient.",
          "Gary hat nie eins davon geschafft. Er sagte, die Fische starren ihn an.",
          "Gary nigdy żadnej nie skończył. Mówił, że ryby się na niego gapią.")),
    (25, ("There. I did not say anything about it. See?", "Pronto. Não falei nada sobre isso. Viu?", "Voilà. Je n'ai rien dit. Tu vois ?",
          "So. Ich habe nichts dazu gesagt. Siehst du?", "No. Nic nie powiedziałam. Widzisz?")),
    (26, ("Clear. I am running out of things to make.", "Limpo. Estou ficando sem coisas para fazer.",
          "Propre. Je commence à manquer de choses à faire.", "Leer. Mir gehen die Sachen zum Bauen aus.",
          "Pusto. Kończą mi się rzeczy do zrobienia.")),
    (27, ("That was a good one. I am not saying which part.", "Esse foi bom. Não vou dizer qual parte.",
          "C'était un bon. Je ne dirai pas quel moment.", "Das war ein gutes. Ich sage nicht, welcher Teil.",
          "Ta była dobra. Nie powiem, która część.")),
    (28, ("Even the roof. You found the way through.", "Até o telhado. Você achou o caminho.", "Même le toit. Tu as trouvé le passage.",
          "Sogar das Dach. Du hast den Weg gefunden.", "Nawet dach. Znalazła się droga.")),
    (29, ("One more. Then there is nothing. Go slowly.", "Mais um. Depois não tem nada. Vá devagar.",
          "Encore un. Après, il n'y a plus rien. Doucement.", "Noch eins. Dann kommt nichts mehr. Langsam.",
          "Jeszcze jedna. Potem nie ma nic. Powoli.")),
):
    note = CLEARED + f" Board {board} of 30."
    if board == 24:
        note += " Gary is one of her exes, now a lump on her side who no longer talks."
    add(f"angler.ava.done.b{board}", words, note)

# THE END OF THE BOARDS. the thirtieth is clear, and she sees that she has become the one who holds on: she made the
# boards to keep the player. she goes, to stay someone, and the game goes back to the way it was. that is the happy
# ending. (angler.ava.out.* and angler.ava.after.* were the old ending, where they sat together forever. unused.)
FREE = WATER + (" The last of the thirty boards is clear. All through the long visit she was afraid that whoever leans on "
                "her stops being their own person, as her exes did. Here she sees that she has started leaning on the "
                "player, so she leaves, warmly, to stay herself. Said one after the other, quietly. Not sad, not a guilt "
                "trip: this is the happy ending.")
add("angler.ava.free.a", ("The last one. I made thirty, so you would stay for thirty.",
                          "O último. Fiz trinta, para você ficar por trinta.",
                          "Le dernier. J'en ai fait trente, pour que tu restes pour trente.",
                          "Das letzte. Ich habe dreißig gemacht, damit du für dreißig bleibst.",
                          "Ostatnia. Zrobiłam trzydzieści, żeby cię zatrzymać na trzydzieści."), FREE)
add("angler.ava.free.b", ("I had started another. I know what that is.",
                          "Eu já tinha começado outro. Sei o que é isso.",
                          "J'en avais commencé un autre. Je sais ce que c'est.",
                          "Ich hatte schon ein neues angefangen. Ich weiß, was das ist.",
                          "Zaczęłam już następną. Wiem, co to znaczy."), FREE)
add("angler.ava.free.c", ("I watched it happen to every one of them.",
                          "Vi acontecer com cada um deles.",
                          "Je l'ai vu arriver à chacun d'eux.",
                          "Ich habe es bei jedem von ihnen gesehen.",
                          "Widziałam to u każdego z nich."), FREE + " 'Them' are her exes, the lumps on her side.")
add("angler.ava.free.d", ("This time it is me holding on. I would like to stay someone too.",
                          "Desta vez sou eu que me seguro. Também quero continuar sendo alguém.",
                          "Cette fois, c'est moi qui m'accroche. Je voudrais rester quelqu'un, moi aussi.",
                          "Diesmal halte ich mich fest. Ich möchte auch jemand bleiben.",
                          "Tym razem to ja się trzymam. A też chcę pozostać kimś."), FREE)
add("angler.ava.free.e", ("So I am going. Not far. Somewhere that is mine.",
                          "Então eu vou. Não para longe. Para um lugar que seja meu.",
                          "Alors je m'en vais. Pas loin. Quelque part qui soit à moi.",
                          "Also gehe ich. Nicht weit. Irgendwohin, das mir gehört.",
                          "Więc odpływam. Niedaleko. Do miejsca, które jest moje."), FREE)
add("angler.ava.free.f", ("I will see you around, friend. At the shop, probably.",
                          "A gente se vê por aí, amigo. Na loja, provavelmente.",
                          "On se verra, l'ami. À la boutique, sans doute.",
                          "Man sieht sich, mein Freund. Im Laden, vermutlich.",
                          "Do zobaczenia, przyjacielu. Pewnie w sklepie."), FREE + " Her last words before she swims away, "
                                                                                    "and the game goes back to the title.")

# and the first time that she is asked anything after that, at the shop or anywhere, she has a life of her own
FREED = BOX + (" The first time the player asks her anything after the happy ending of the long visit, where she left to "
               "stay her own person and said she would see them around. She is doing well. Light and dry.")
add("angler.ava.freed.a", ("There you are. Around, like I said.",
                           "Aí está você. Por aí, como eu disse.",
                           "Te voilà. Dans les parages, comme promis.",
                           "Da bist du. Man sieht sich, wie gesagt.",
                           "Jesteś. Tak jak mówiłam, gdzieś po drodze."), FREED)
add("angler.ava.freed.b", ("I have been busy. Things of my own. You would not like them, and that is the best part.",
                           "Andei ocupada. Coisas minhas. Você não ia gostar, e essa é a melhor parte.",
                           "J'ai été occupée. Des choses à moi. Ça ne te plairait pas, et c'est le meilleur.",
                           "Ich hatte zu tun. Eigene Sachen. Dir würden sie nicht gefallen, und das ist das Beste daran.",
                           "Byłam zajęta. Swoimi sprawami. Nie spodobałyby ci się, i to jest w nich najlepsze."), FREED)

OUT = WATER + (" The last of the thirty boards is clear, and she has no more. The boards were her reason for the player "
               "to stay. Said one after the other, quietly. Nothing happens after this.")
add("angler.ava.out.a", ("That was the last one. I'm out of ideas.", "Esse foi o último. Fiquei sem ideias.",
                         "C'était le dernier. Je n'ai plus d'idées.", "Das war das letzte. Mir fällt nichts mehr ein.",
                         "To była ostatnia. Skończyły mi się pomysły."), OUT)
add("angler.ava.out.b", ("I made them so you'd have a reason. You can go now. It's fine.",
                         "Fiz para você ter um motivo. Pode ir agora. Tudo bem.",
                         "Je les ai faits pour te donner une raison. Tu peux partir. Ça va.",
                         "Ich hab sie gemacht, damit du einen Grund hast. Du kannst gehen. Schon gut.",
                         "Zrobiłam je, żeby był jakiś powód. Możesz już iść. Nic się nie stało."), OUT)

MENU = {
    "menu.hint.main.bottle": (("{dir} choose    {confirm} select    {info} the bottle",
                               "{dir} escolher    {confirm} selecionar    {info} a garrafa",
                               "{dir} choisir    {confirm} valider    {info} la bouteille",
                               "{dir} wählen    {confirm} bestätigen    {info} die Flasche",
                               "{dir} wybierz    {confirm} zatwierdź    {info} butelka"),
                              "The words follow button icons. The main menu's hint while a bottle with a letter in it "
                              "floats on the water. Keep it as long as menu.hint.main at most, plus the bottle."),
}

ABILITY = {
    "ability.angler.gone": (("The light is out. Nobody is down there now. This card does nothing.",
                             "A luz se apagou. Não há ninguém lá embaixo agora. Esta carta não faz nada.",
                             "La lumière s'est éteinte. Il n'y a plus personne en bas. Cette carte ne fait rien.",
                             "Das Licht ist aus. Dort unten ist niemand mehr. Diese Karte bewirkt nichts.",
                             "Światło zgasło. Na dole nikogo już nie ma. Ta karta nic nie daje."),
                            "The card 'bright idea' after Ava, the anglerfish who answered questions, has left the game "
                            "for good. The card is still dealt, but taking it does nothing, and this must say so plainly. "
                            "Flat and a little sad. Descriptions wrap at about 124 px."),
}


def append(table, lines):
    path = LANG / table
    raw = path.read_bytes().decode("utf-8")
    have = {r[0] for r in csv.reader(io.StringIO(raw)) if r}
    out = io.StringIO()
    w = csv.writer(out, lineterminator="\n")
    added = 0
    for key, ((en, pt, fr, de, pl), note) in lines.items():
        if key not in have:
            w.writerow([key, en, pt, note, "", "", "FALSE", "", fr, de, pl, "FALSE", "FALSE", "FALSE", "", "", ""])
            added += 1
    if not raw.endswith("\n"):
        raw += "\n"
    path.write_bytes((raw + out.getvalue()).encode("utf-8"))
    print(f"{table}: added {added} rows")


def main():
    for key, (words, _) in {**ANGLER, **MENU, **ABILITY}.items():
        for w in words:
            low = w.lower()
            assert " free" not in low and "love" not in low, key
        assert all(("{$code}" in w) == ("{$code}" in words[0]) for w in words), key
        for icon in ("{info}", "{confirm}", "{dir}"):
            assert all((icon in w) == (icon in words[0]) for w in words), key
    append("angler.csv", ANGLER)
    append("menu.csv", MENU)
    append("ability.csv", ABILITY)


if __name__ == "__main__":
    main()
