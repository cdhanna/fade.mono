"""The conversations of the long visit, in every language, and what makes them.

This is the one place that Ava's talks are written down. It writes Fade.MonoGame/fish_puzzle_talk.fbasic,
and adds any row that angler.csv does not have yet. A row that the sheet already has keeps the sheet's
words, because a sync puts them back: a reworded line needs a new key.

There is a talk after every fifth board, in the angler's usual scene: she says two things, the player
picks one of two replies, she answers that, and she says one more thing. See fish_headers_puzzle.

    py -I build/make_ava_talk.py

Rules for every line: she never says "free" or "love". She never guilts the player for going. Every
rule of the game that she states is true. She dodges rather than lies. She is FEMALE, and nothing
may give away the player's gender, which takes care in the past tense in French and Polish.
"""

import csv
import io
import pathlib

GAME = pathlib.Path(__file__).resolve().parent.parent / "Fade.MonoGame"
OUT = GAME / "fish_puzzle_talk.fbasic"
TABLE = GAME / "Assets" / "Fish" / "Lang" / "angler.csv"

NOTE = ("Ava, the anglerfish, in the secret ending where the player stays with her. She is FEMALE: use feminine "
        "forms for her. Never reveal the player's gender. Dry and calm, secretly fond. Never say 'free' or 'love'. "
        "Shown in the angler's box under the board, three lines at most.")
REPLY_NOTE = ("A reply the silent player can pick when talking to Ava. Very short. It must not reveal "
              "the player's gender.")

# en, pt-BR, fr, de, pl
REPLIES = {
    "mm": ("Mm.", "Hum.", "Mm.", "Mm.", "Mm."),
    "why": ("Why?", "Por quê?", "Pourquoi ?", "Warum?", "Dlaczego?"),
    "more": ("Tell me more.", "Conte mais.", "Dis-m'en plus.", "Erzähl mehr.", "Powiedz więcej."),
    "dots": ("...", "...", "...", "...", "..."),
}

HINT_NOTE = "Button hints along the bottom of the puzzle board, side by side. Very short."
HINT2 = ("{info} start over   {move1} undo", "{info} recomeçar   {move1} desfazer", "{info} recommencer   {move1} annuler",
         "{info} neu anfangen   {move1} zurück", "{info} od nowa   {move1} cofnij")

# words for the first boards, and for being stuck. they sit in fish_routines_puzzle, by key.
LINES = {
    "leave.a": ("You said you would stay.", "Você disse que ficaria.", "Tu avais dit que tu resterais.",
                "Du hast gesagt, du bleibst.", "Podobno zostajesz."),
    "leave.b": ("Are you leaving?", "Você vai embora?", "Tu t'en vas ?", "Gehst du?", "Odchodzisz?"),
    "reply.stay": ("No. I'm staying.", "Não. Vou ficar.", "Non. Je reste.", "Nein. Ich bleibe.", "Nie. Zostaję."),
    "reply.go": ("Yes. I'm leaving.", "Sim. Vou embora.", "Oui. Je m'en vais.", "Ja. Ich gehe.", "Tak. Odchodzę."),
    "stay.a": ("Mm. Your board is where you left it.", "Hum. Seu tabuleiro está onde você deixou.",
               "Mm. Ton plateau est là où tu l'as laissé.", "Mm. Dein Brett liegt noch da, wo es lag.",
               "Mm. Twoja plansza czeka tam, gdzie była."),
    "go.a": ("Oh.", "Ah.", "Oh.", "Oh.", "Och."),
    "go.b": ("No, that is good. That is what I wanted for you.", "Não, isso é bom. É o que eu queria para você.",
             "Non, c'est bien. C'est ce que je voulais pour toi.", "Nein, das ist gut. Das wollte ich für dich.",
             "Nie, to dobrze. Tego dla ciebie chciałam."),
    "go.c": ("I will send word. Sometimes. Do not wait for it.", "Vou mandar notícias. Às vezes. Não fique esperando.",
             "J'enverrai des nouvelles. Parfois. Ne les attends pas.", "Ich schicke dir Nachricht. Manchmal. Warte nicht darauf.",
             "Czasem się odezwę. Nie czekaj na to."),
    "go.d": ("Go on, friend. I will keep your place.", "Vai, amigo. Vou guardar seu lugar.",
             "Va, l'ami. Je garderai ta place.", "Geh nur, mein Freund. Ich halte dir den Platz frei.",
             "Idź, przyjacielu. Zachowam ci miejsce."),
    "stuck2": ("Those ones will never find each other now. {move1} takes that back, friend.",
               "Esses nunca mais vão se encontrar. {move1} desfaz isso, amigo.",
               "Ceux-là ne se retrouveront jamais. {move1} annule ça, l'ami.",
               "Die finden nie mehr zueinander. {move1} macht das rückgängig, mein Freund.",
               "Te już się nie odnajdą. {move1} to cofnie, przyjacielu."),
    "d5.a3": ("Everyone who came this close stopped wanting things of their own, you know.",
              "Todo mundo que chegou tão perto parou de querer coisas próprias, sabia?",
              "Tous ceux qui sont venus si près ont cessé de vouloir des choses à eux, tu sais.",
              "Alle, die so nah kamen, wollten irgendwann nichts Eigenes mehr, weißt du.",
              "Każdy, kto był tak blisko, przestawał chcieć czegoś dla siebie, wiesz?"),
    "d2.b2": ("They do not come back when they pop. So look before you swap.",
              "Eles não voltam quando estouram. Então olhe antes de trocar.",
              "Ils ne reviennent pas quand ils éclatent. Alors regarde avant d'échanger.",
              "Sie kommen nicht wieder, wenn sie platzen. Also schau, bevor du tauschst.",
              "Nie wracają, gdy pękną. Więc patrz, zanim zamienisz."),
    "d3.a2": ("I keep expecting you to stop.",
              "Continuo esperando que você pare.",
              "Je m'attends toujours à ce que tu arrêtes.",
              "Ich rechne ständig damit, dass du aufhörst.",
              "Ciągle czekam, aż przestaniesz."),
    "d3.b2": ("Think before the first swap. I will wait.",
              "Pense antes da primeira troca. Eu espero.",
              "Réfléchis avant le premier échange. J'attendrai.",
              "Denk vor dem ersten Tausch nach. Ich warte.",
              "Pomyśl przed pierwszą zamianą. Poczekam."),
    "d3.done2": ("Hm. You did not stop.",
                 "Hum. Você não parou.",
                 "Hm. Tu n'as pas arrêté.",
                 "Hm. Du hast nicht aufgehört.",
                 "Hm. Nie przestajesz."),
    "d5.a2": ("That is nearly five. I have a few things to say after this one.",
              "Já são quase cinco. Tenho umas coisas a dizer depois deste.",
              "Ça fait presque cinq. J'ai deux ou trois choses à dire après celui-ci.",
              "Das sind fast fünf. Danach habe ich ein paar Dinge zu sagen.",
              "To już prawie pięć. Po tej mam kilka rzeczy do powiedzenia."),
    "d7.done2": ("Still here. Noted.",
                 "Ainda aqui. Anotado.",
                 "Encore là. C'est noté.",
                 "Immer noch hier. Notiert.",
                 "Wciąż tutaj. Odnotowano."),
}

# a line of a talk that has been written again gets a new key, because a sync would put the old words back.
# the talks below have the new words. this says which ones those are, (talk, part), and what goes on the end of the
# key: 2 for the second words, 3 for the third. talks 6 and 19 were about the moon, and are about dependence now.
REWRITTEN = {(1, "a"): "2", (2, "a"): "2", (2, "b"): "2", (4, "a"): "2", (5, "a"): "2", (5, "end"): "2",
             (7, "a"): "2", (7, "b"): "2", (7, "r2"): "2", (8, "a"): "2", (12, "a"): "2", (12, "b"): "2",
             (15, "a"): "2"}
for _part in ("a", "b", "r1", "r2", "end"):
    REWRITTEN[(6, _part)] = "3"
    REWRITTEN[(19, _part)] = "3"

# what she says as a board comes in, on the boards with no words of their own, by stage of the arc
REMARKS = {
    "g1": ("Here.", "Aqui.", "Tiens.", "Hier.", "Proszę."),
    "g2": ("Another, then.", "Outro, então.", "Un autre, alors.", "Noch eins also.", "Zatem następna."),
    "g3": ("Over there, friend.", "Aí mesmo, amigo.", "Reste là-bas, l'ami.", "Bleib da drüben, mein Freund.",
           "Tam zostań, przyjacielu."),
    "r1": ("I worked on this one.", "Caprichei neste.", "J'ai travaillé sur celui-ci.",
           "An diesem habe ich gearbeitet.", "Nad tą trochę pracowałam."),
    "r2": ("You again. Good.", "Você de novo. Que bom.", "Encore toi. Bien.", "Du schon wieder. Gut.",
           "Znowu ty. Dobrze."),
    "r3": ("Mind the gaps.", "Cuidado com os vãos.", "Attention aux trous.", "Achte auf die Lücken.",
           "Uważaj na luki."),
    "c1": ("This one is quiet.", "Este é quieto.", "Celui-ci est calme.", "Das hier ist still.",
           "Ta jest cicha."),
    "c2": ("Still you?", "Ainda é você?", "Toujours toi ?", "Immer noch du?", "Wciąż ty?"),
    "c3": ("I made this one slowly.", "Fiz este devagar.", "J'ai fait celui-ci lentement.",
           "Das hier habe ich langsam gemacht.", "Tę robiłam powoli."),
    "o1": ("No rush.", "Sem pressa.", "Pas de hâte.", "Keine Eile.", "Bez pośpiechu."),
    "o2": ("Take your time.", "Não tenha pressa.", "Prends ton temps.", "Lass dir Zeit.", "Nie spiesz się."),
    "o3": ("Slowly, please.", "Devagar, por favor.", "Doucement, s'il te plaît.", "Langsam, bitte.",
           "Powoli, proszę."),
}

# one talk: its first two lines, the two replies with her answer to each, and the last line
TALKS = [
 # GUEST: she is managing a clinger
 (("Some ground rules, now that you seem to be staying. You stay over there, and I stay over here.",
   "Algumas regras, já que você parece estar ficando. Você fica aí, e eu fico aqui.",
   "Quelques règles, puisque tu sembles rester. Tu restes là-bas, et je reste ici.",
   "Ein paar Regeln, da du anscheinend bleibst. Du bleibst da drüben, und ich bleibe hier.",
   "Kilka zasad, skoro chyba zostajesz. Ty tam, a ja tutaj."),
  ("Do not get attached. I mean that literally, friend.",
   "Não se apegue. Falo no sentido literal, amigo.",
   "Ne t'accroche pas. Je parle au sens propre, l'ami.",
   "Häng dich nicht an mich. Das meine ich wörtlich, mein Freund.",
   "Nie przyczepiaj się. Mówię dosłownie, przyjacielu."),
  "why", ("Because it has happened. Look at my side.",
   "Porque já aconteceu. Olhe para o meu lado.",
   "Parce que c'est arrivé. Regarde mon flanc.",
   "Weil es schon passiert ist. Sieh dir meine Seite an.",
   "Bo to już się zdarzyło. Spójrz na mój bok."),
  "mm", ("Good. A listener. Those last longer.",
   "Bom. Alguém que escuta. Esses duram mais.",
   "Bien. Quelqu'un qui écoute. Ça dure plus longtemps.",
   "Gut. Jemand, der zuhört. Die halten länger.",
   "Dobrze. Ktoś, kto słucha. Tacy dłużej wytrzymują."),
  ("Right. Back to the boards.",
   "Certo. De volta aos tabuleiros.",
   "Bien. Retour aux plateaux.",
   "Also. Zurück zu den Brettern.",
   "Dobrze. Wracajmy do plansz.")),

 (("You are looking at my side. Everyone does, sooner or later.",
   "Você está olhando para o meu lado. Todo mundo olha, cedo ou tarde.",
   "Tu regardes mon flanc. Tout le monde finit par le faire.",
   "Du siehst auf meine Seite. Das tun alle, früher oder später.",
   "Patrzysz na mój bok. Wszyscy w końcu patrzą."),
  ("Those lumps? Exes. That one is Gary. He has opinions. Had.",
   "Esses caroços? Ex-namorados. Aquele é o Gary. Ele tem opiniões. Tinha.",
   "Ces bosses ? Des ex. Lui, c'est Gary. Il a des opinions. Avait.",
   "Diese Beulen? Exfreunde. Das ist Gary. Er hat Meinungen. Hatte.",
   "Te guzy? Byli. To Gary. Ma poglądy. Miał."),
  "more", ("Gary thought the sea was too wet. He said so often, until he did not.",
   "Gary achava o mar molhado demais. Dizia isso sempre, até parar de dizer.",
   "Gary trouvait la mer trop mouillée. Il le disait souvent, puis plus du tout.",
   "Gary fand das Meer zu nass. Das sagte er oft, bis er es nicht mehr sagte.",
   "Gary uważał, że morze jest za mokre. Mówił to często, aż przestał."),
  "dots", ("Yes. That is about what Gary says now.",
   "Sim. É mais ou menos o que o Gary diz agora.",
   "Oui. C'est à peu près ce que dit Gary maintenant.",
   "Ja. Das ist ungefähr, was Gary heute sagt.",
   "Tak. Mniej więcej tyle mówi teraz Gary."),
  ("I have another. If you are staying.",
   "Tenho outro. Se você ficar.",
   "J'en ai un autre. Si tu restes.",
   "Ich habe noch eins. Falls du bleibst.",
   "Mam następną. Jeśli zostajesz.")),

 (("You popped every one of those. Do you enjoy it, or are you being polite?",
   "Você estourou todos. Você gosta, ou é educação?",
   "Tu les as tous fait éclater. Ça te plaît, ou c'est par politesse ?",
   "Du hast alle platzen lassen. Macht dir das Spaß, oder ist das Höflichkeit?",
   "Rozbijasz je wszystkie. Lubisz to, czy to tylko uprzejmość?"),
  ("It does not matter. I made them for you to pop.",
   "Não importa. Fiz para você estourar.",
   "Peu importe. Je les ai faits pour ça.",
   "Egal. Ich habe sie zum Platzen gemacht.",
   "Nieważne. Zrobiłam je do rozbijania."),
  "why", ("Something has to happen down here. Mostly nothing does.",
   "Alguma coisa tem que acontecer aqui. Quase nada acontece.",
   "Il faut bien qu'il se passe quelque chose. D'habitude, rien.",
   "Hier unten muss etwas passieren. Meistens passiert nichts.",
   "Coś musi się tu dziać. Zwykle nic się nie dzieje."),
  "mm", ("Polite, then.",
   "Educação, então.",
   "Par politesse, donc.",
   "Also Höflichkeit.",
   "Czyli uprzejmość."),
  ("Stay over there, friend.",
   "Fique aí, amigo.",
   "Reste là-bas, l'ami.",
   "Bleib da drüben, mein Freund.",
   "Zostań tam, przyjacielu.")),

 (("Twenty boards, and you have not tried to bite me.",
   "Vinte tabuleiros, e você não tentou me morder.",
   "Vingt plateaux, et tu n'as pas essayé de me mordre.",
   "Zwanzig Bretter, und du hast nicht versucht, mich zu beißen.",
   "Dwadzieścia plansz, a ty nie próbujesz mnie ugryźć."),
  ("I keep checking. It is a habit.",
   "Continuo conferindo. É costume.",
   "Je vérifie sans cesse. C'est une habitude.",
   "Ich prüfe das ständig. Reine Gewohnheit.",
   "Ciągle sprawdzam. Taki nawyk."),
  "why", ("Everybody bites. It is what the small ones come for. You just press things.",
   "Todos mordem. Os pequenos vêm para isso. Você só aperta coisas.",
   "Tout le monde mord. Les petits viennent pour ça. Toi, tu appuies sur des trucs.",
   "Alle beißen. Dafür kommen die Kleinen. Du drückst nur Sachen.",
   "Wszyscy gryzą. Po to przypływają ci mali. Ty tylko coś naciskasz."),
  "dots", ("Fine. Keep your secrets. Keep your teeth.",
   "Tudo bem. Guarde seus segredos. Guarde seus dentes.",
   "Bien. Garde tes secrets. Garde tes dents.",
   "Schön. Behalte deine Geheimnisse. Behalte deine Zähne.",
   "Dobrze. Zachowaj sekrety. Zachowaj zęby."),
  ("That was a joke. Mostly.",
   "Foi brincadeira. Quase.",
   "C'était une blague. En gros.",
   "Das war ein Scherz. Größtenteils.",
   "To był żart. Głównie.")),

 # REGULAR: a habit that she looks forward to, and will not say so
 (("You pop these like you have done it before. So I looked up your old games.",
   "Você estoura como quem já fez isso antes. Então fui ver seus jogos antigos.",
   "Tu les fais éclater comme si tu avais l'habitude. Alors j'ai regardé tes anciennes parties.",
   "Du lässt sie platzen, als hättest du Übung. Also habe ich deine alten Spiele angesehen.",
   "Rozbijasz je, jakby to nie był pierwszy raz. Więc sprawdziłam twoje stare gry."),
  ("A multiplier of {$score}. All those fish, and for what?",
   "Um multiplicador de {$score}. Tantos peixes, e para quê?",
   "Un multiplicateur de {$score}. Tous ces poissons, et pour quoi ?",
   "Ein Multiplikator von {$score}. All die Fische, und wofür?",
   "Mnożnik {$score}. Tyle ryb, i po co?"),
  "more", ("I am not judging. I only like a big number I cannot do anything with.",
   "Não estou julgando. Só gosto de um número grande que não serve para nada.",
   "Je ne juge pas. J'aime juste un grand nombre qui ne sert à rien.",
   "Ich urteile nicht. Ich mag nur große Zahlen, mit denen ich nichts anfangen kann.",
   "Nie oceniam. Po prostu lubię duże liczby, z którymi nic nie mogę zrobić."),
  "why", ("Because you gave it up to sit here. I would like it to have been worth something.",
   "Porque você largou isso para ficar aqui. Eu queria que tivesse valido algo.",
   "Parce que tu as tout laissé pour être ici. J'aimerais que ça ait valu quelque chose.",
   "Weil du das aufgegeben hast, um hier zu sitzen. Es soll etwas wert gewesen sein.",
   "Bo to porzucasz, żeby tu siedzieć. Wolałabym, żeby to było coś warte."),
  ("Anyway. Mine have fewer numbers. I worked on the next one.",
   "Enfim. Os meus têm menos números. Caprichei no próximo.",
   "Bref. Les miens ont moins de chiffres. J'ai travaillé sur le suivant.",
   "Jedenfalls. Meine haben weniger Zahlen. Am nächsten habe ich gearbeitet.",
   "W każdym razie. Moje mają mniej liczb. Nad następną pracowałam.")),

 # she wants the player to keep a life of their own, and keeps her hands off it
 (("Tell me something you did up there. Something I was not part of.",
   "Me conte algo que você fez lá em cima. Algo de que eu não fiz parte.",
   "Raconte-moi quelque chose que tu as fait là-haut. Quelque chose sans moi.",
   "Erzähl mir etwas, das du da oben gemacht hast. Etwas ohne mich.",
   "Opowiedz mi coś, co robisz tam na górze. Coś beze mnie."),
  ("No. Do not. I only like knowing that there is some.",
   "Não. Não conte. Só gosto de saber que existe.",
   "Non. Ne dis rien. J'aime juste savoir qu'il y en a.",
   "Nein. Erzähl es nicht. Ich mag nur wissen, dass es so etwas gibt.",
   "Nie. Nie mów. Lubię po prostu wiedzieć, że coś takiego jest."),
  "why", ("Because the ones on my side ran out of things of their own. You still have some.",
   "Porque os do meu lado ficaram sem nada de seu. Você ainda tem.",
   "Parce que ceux sur mon flanc n'avaient plus rien à eux. Toi, tu as encore quelque chose.",
   "Weil denen an meiner Seite nichts Eigenes mehr blieb. Du hast noch etwas.",
   "Bo tym na moim boku nie zostało nic własnego. Ty jeszcze coś masz."),
  "mm", ("Keep it, then. It is yours.",
   "Então guarde. É seu.",
   "Garde-le, alors. C'est à toi.",
   "Dann behalte es. Es gehört dir.",
   "To zachowaj to. Jest twoje."),
  ("Good. Here.",
   "Bom. Tome.",
   "Bien. Tiens.",
   "Gut. Hier.",
   "Dobrze. Proszę.")),

 (("The water goes very still when you stop to think.",
   "A água fica bem parada quando você para para pensar.",
   "L'eau devient très calme quand tu t'arrêtes pour réfléchir.",
   "Das Wasser wird sehr still, wenn du nachdenkst.",
   "Woda bardzo się uspokaja, gdy przystajesz, żeby pomyśleć."),
  ("I do not mind it.",
   "Eu não me importo.",
   "Ça ne me gêne pas.",
   "Das stört mich nicht.",
   "Nie przeszkadza mi to."),
  "dots", ("Neither did you, apparently.",
   "Você também não, pelo visto.",
   "Toi non plus, apparemment.",
   "Dich offenbar auch nicht.",
   "Tobie też nie, jak widać."),
  "why", ("I said I do not mind. That is the end of that.",
   "Eu disse que não me importo. Fim de papo.",
   "J'ai dit que ça ne me gênait pas. Point final.",
   "Ich sagte, es stört mich nicht. Ende der Sache.",
   "Mówię, że mi nie przeszkadza. Koniec tematu."),
  ("Here. I made it harder. Or easier. I forget which you prefer.",
   "Tome. Fiz mais difícil. Ou mais fácil. Esqueci qual você prefere.",
   "Tiens. Je l'ai fait plus dur. Ou plus facile. J'oublie ce que tu préfères.",
   "Hier. Ich habe es schwerer gemacht. Oder leichter. Ich vergesse, was du magst.",
   "Proszę. Zrobiłam trudniejszą. Albo łatwiejszą. Zapomniałam, co wolisz.")),

 (("I was thinking about Gary. He told stories, once. Long ones, about a trench.",
   "Eu estava pensando no Gary. Ele contava histórias. Longas, sobre uma fossa.",
   "Je pensais à Gary. Il racontait des histoires. Longues, sur une fosse.",
   "Ich habe an Gary gedacht. Er hat Geschichten erzählt. Lange, über einen Graben.",
   "Myślałam o Garym. Kiedyś opowiadał historie. Długie, o rowie."),
  ("He started one the day he arrived. I never found out how it ended.",
   "Começou uma no dia em que chegou. Nunca soube como terminava.",
   "Il en a commencé une en arrivant. Je n'ai jamais su la fin.",
   "Eine begann er am Tag seiner Ankunft. Das Ende habe ich nie erfahren.",
   "Jedną zaczął w dniu przybycia. Nigdy nie poznałam końca."),
  "more", ("Neither can I. That is what I am telling you.",
   "Nem eu posso. É isso que estou dizendo.",
   "Moi non plus. C'est ce que je te dis.",
   "Ich auch nicht. Das sage ich dir ja.",
   "Ja też nie. To właśnie ci mówię."),
  "dots", ("Yes. I sat with that for a while too.",
   "Sim. Também fiquei um tempo com isso.",
   "Oui. Moi aussi, je suis restée un moment là-dessus.",
   "Ja. Dabei bin ich auch lange geblieben.",
   "Tak. Też długo z tym siedziałam."),
  ("Never mind. Next.",
   "Deixa pra lá. Próximo.",
   "Laisse tomber. Au suivant.",
   "Schon gut. Weiter.",
   "Nieważne. Następna.")),

 (("I have started setting these up in the order I think you would like.",
   "Comecei a montar estes na ordem que acho que você gostaria.",
   "Je les prépare maintenant dans l'ordre qui, je crois, te plairait.",
   "Ich lege sie jetzt in der Reihenfolge an, die dir gefallen könnte.",
   "Zaczęłam je układać w kolejności, która chyba ci się spodoba."),
  ("Do not read anything into it.",
   "Não tire conclusões.",
   "N'y vois rien de particulier.",
   "Interpretier da nichts hinein.",
   "Nie doszukuj się w tym niczego."),
  "mm", ("You are reading into it.",
   "Você está tirando conclusões.",
   "Tu y vois quelque chose.",
   "Du interpretierst etwas hinein.",
   "Doszukujesz się."),
  "why", ("I said do not.",
   "Eu disse para não tirar.",
   "J'ai dit non.",
   "Ich sagte: nicht.",
   "Powiedziałam: nie."),
  ("Anyway.",
   "Enfim.",
   "Bref.",
   "Jedenfalls.",
   "W każdym razie.")),

 (("That is half of them. Half of what, you ask. Never mind.",
   "Isso é metade. Metade do quê, você pergunta. Deixa pra lá.",
   "Ça fait la moitié. La moitié de quoi, dis-tu. Laisse tomber.",
   "Das ist die Hälfte. Wovon, fragst du. Schon gut.",
   "To połowa. Połowa czego, pytasz. Nieważne."),
  ("Do you know you are the first one to still be talking back?",
   "Sabia que você é a primeira pessoa que ainda responde?",
   "Tu sais que personne d'autre ne me répond encore ?",
   "Weißt du, dass sonst niemand mehr antwortet?",
   "Wiesz, że nikt inny już mi nie odpowiada?"),
  "mm", ("You see? That counts. Barely.",
   "Viu? Isso conta. Por pouco.",
   "Tu vois ? Ça compte. À peine.",
   "Siehst du? Das zählt. Gerade so.",
   "Widzisz? To się liczy. Ledwo."),
  "dots", ("That also counts. I have lowered the bar.",
   "Isso também conta. Baixei o nível.",
   "Ça compte aussi. J'ai baissé mes exigences.",
   "Das zählt auch. Ich habe die Latte gesenkt.",
   "To też się liczy. Obniżyłam poprzeczkę."),
  ("Friend.",
   "Amigo.",
   "L'ami.",
   "Mein Freund.",
   "Przyjacielu.")),

 # COMPANY: the first one who stayed themselves, and why that frightens her
 (("Gary told a very good joke once.",
   "Gary contou uma piada muito boa, uma vez.",
   "Gary a raconté une très bonne blague, un jour.",
   "Gary hat einmal einen sehr guten Witz erzählt.",
   "Gary opowiedział kiedyś bardzo dobry dowcip."),
  ("I have been trying to remember it for years.",
   "Faz anos que tento lembrar.",
   "J'essaie de m'en souvenir depuis des années.",
   "Seit Jahren versuche ich, mich zu erinnern.",
   "Od lat próbuję go sobie przypomnieć."),
  "why", ("Because he cannot tell it again.",
   "Porque ele não pode contar de novo.",
   "Parce qu'il ne peut plus la raconter.",
   "Weil er ihn nicht noch einmal erzählen kann.",
   "Bo on już go nie opowie."),
  "more", ("A crab walks into something. That is all I have.",
   "Um caranguejo entra em algum lugar. É só o que lembro.",
   "Un crabe entre quelque part. C'est tout ce que j'ai.",
   "Eine Krabbe geht irgendwohin. Mehr habe ich nicht.",
   "Krab gdzieś wchodzi. Tylko tyle pamiętam."),
  ("Anyway. A board.",
   "Enfim. Um tabuleiro.",
   "Bref. Un plateau.",
   "Jedenfalls. Ein Brett.",
   "W każdym razie. Plansza.")),

 (("The ones on my side did not mean any harm, you know. They only held on.",
   "Os do meu lado não queriam fazer mal, sabia? Só se seguraram.",
   "Ceux sur mon flanc ne voulaient pas de mal, tu sais. Ils se sont juste accrochés.",
   "Die an meiner Seite wollten nichts Böses, weißt du. Sie haben sich nur festgehalten.",
   "Ci na moim boku nie chcieli nic złego, wiesz? Po prostu się trzymali."),
  ("Then one day they only wanted what I wanted. Each of them.",
   "Até que um dia só queriam o que eu queria. Cada um deles.",
   "Puis un jour, ils ne voulaient plus que ce que je voulais. Chacun d'eux.",
   "Und eines Tages wollten sie nur noch, was ich wollte. Jeder von ihnen.",
   "Aż pewnego dnia chcieli już tylko tego, czego ja chciałam. Każdy z nich."),
  "why", ("That is what holding on does. I cannot tell you when it happens. I have looked.",
   "É o que segurar faz. Não sei dizer quando acontece. Já procurei.",
   "C'est ce que fait le fait de s'accrocher. Je ne sais pas quand ça arrive. J'ai cherché.",
   "Das macht das Festhalten. Wann es passiert, weiß ich nicht. Ich habe gesucht.",
   "To właśnie robi trzymanie się. Nie wiem kiedy. Szukałam."),
  "dots", ("Yes. There is nothing to say. They found that too.",
   "Sim. Não há o que dizer. Eles também descobriram.",
   "Oui. Il n'y a rien à dire. Eux aussi l'ont découvert.",
   "Ja. Da gibt es nichts zu sagen. Das fanden sie auch.",
   "Tak. Nie ma co mówić. Oni też to odkryli."),
  ("I would rather not talk now. Here.",
   "Prefiro não falar agora. Tome.",
   "Je préfère ne plus parler. Tiens.",
   "Ich rede jetzt lieber nicht. Hier.",
   "Wolę teraz nie mówić. Proszę.")),

 (("If you started to go quiet, you would tell me?",
   "Se você começasse a se calar, me diria?",
   "Si tu commençais à te taire, tu me le dirais ?",
   "Wenn du still würdest, würdest du es mir sagen?",
   "Gdyby z twojej strony nastała cisza, powiesz mi?"),
  ("I only ask because I would not be able to tell.",
   "Só pergunto porque eu não saberia notar.",
   "Je demande parce que je ne saurais pas le voir.",
   "Ich frage nur, weil ich es nicht merken würde.",
   "Pytam, bo sama bym tego nie poznała."),
  "mm", ("That is what worries me.",
   "É isso que me preocupa.",
   "C'est ce qui m'inquiète.",
   "Das ist es, was mir Sorgen macht.",
   "To mnie właśnie martwi."),
  "more", ("There is no more. That was the whole thing.",
   "Não há mais. Era só isso.",
   "Il n'y a rien de plus. C'était tout.",
   "Mehr gibt es nicht. Das war alles.",
   "Nie ma więcej. To było wszystko."),
  ("Press something, so I know.",
   "Aperte alguma coisa, para eu saber.",
   "Appuie sur quelque chose, que je sache.",
   "Drück etwas, damit ich es weiß.",
   "Naciśnij coś, żebym wiedziała."),),

 (("Do you miss it up there? No. Do not answer.",
   "Sente falta de lá de cima? Não. Não responda.",
   "Ça te manque, là-haut ? Non. Ne réponds pas.",
   "Fehlt es dir da oben? Nein. Antworte nicht.",
   "Tęsknisz za tym, co na górze? Nie. Nie odpowiadaj."),
  ("I only ask because I cannot make the boards as good as that.",
   "Só pergunto porque não consigo fazer tabuleiros tão bons.",
   "Je demande parce que mes plateaux ne seront jamais aussi bons.",
   "Ich frage nur, weil meine Bretter nie so gut sein werden.",
   "Pytam, bo moje plansze nigdy nie będą tak dobre."),
  "mm", ("That was not an answer, as I asked. Thank you.",
   "Isso não foi resposta, como pedi. Obrigada.",
   "Ce n'était pas une réponse, comme demandé. Merci.",
   "Das war keine Antwort, wie gewünscht. Danke.",
   "To nie była odpowiedź, jak prosiłam. Dziękuję."),
  "why", ("Because I know what I am up against.",
   "Porque sei contra o que estou competindo.",
   "Parce que je sais à quoi je me mesure.",
   "Weil ich weiß, womit ich mich messe.",
   "Bo wiem, z czym się mierzę."),
  ("I could make them harder. Or easier. Which keeps you?",
   "Posso fazer mais difíceis. Ou mais fáceis. O que faz você ficar?",
   "Je peux les faire plus durs. Ou plus faciles. Qu'est-ce qui te retient ?",
   "Ich könnte sie schwerer machen. Oder leichter. Was hält dich hier?",
   "Mogę je utrudnić. Albo ułatwić. Co cię zatrzyma?")),

 (("I keep calling you friend. Did you notice?",
   "Eu vivo chamando você de amigo. Percebeu?",
   "Je n'arrête pas de t'appeler l'ami. Tu as remarqué ?",
   "Ich nenne dich ständig mein Freund. Ist dir das aufgefallen?",
   "Ciągle mówię do ciebie przyjacielu. Widać to?"),
  ("It started as a fence. I do not know what it is now.",
   "Começou como uma cerca. Não sei o que é agora.",
   "C'était une clôture, au début. Je ne sais plus ce que c'est.",
   "Es begann als Zaun. Ich weiß nicht, was es jetzt ist.",
   "Zaczęło się jako płot. Nie wiem, czym jest teraz."),
  "why", ("I said it to keep you out, and here you are anyway.",
   "Eu dizia para manter você longe, e aqui está você.",
   "Je le disais pour te tenir à l'écart, et te voilà quand même.",
   "Ich sagte es, um dich fernzuhalten, und trotzdem bist du hier.",
   "Mówiłam to, by trzymać cię z dala, a jednak tu jesteś."),
  "dots", ("No, you would not say. That is why I can tell you.",
   "Não, você não diria. Por isso posso contar.",
   "Non, tu ne dirais rien. C'est pour ça que je peux te le dire.",
   "Nein, du würdest nichts sagen. Darum kann ich es dir erzählen.",
   "Nie, nic nie powiesz. Dlatego mogę ci to mówić."),
  ("Never mind. Back to it.",
   "Deixa pra lá. Voltemos.",
   "Laisse tomber. On reprend.",
   "Schon gut. Weiter geht's.",
   "Nieważne. Wracajmy.")),

 (("They all stayed, and that was the trouble.",
   "Todos ficaram, e esse foi o problema.",
   "Ils sont tous restés, et c'était bien le problème.",
   "Sie sind alle geblieben, und das war das Problem.",
   "Wszyscy zostali i w tym był problem."),
  ("You stay differently. I do not understand how.",
   "Você fica de um jeito diferente. Não entendo como.",
   "Toi, tu restes autrement. Je ne comprends pas comment.",
   "Du bleibst anders. Ich verstehe nicht, wie.",
   "Ty zostajesz inaczej. Nie rozumiem jak."),
  "more", ("You go on being someone. Every time I look.",
   "Você continua sendo alguém. Toda vez que olho.",
   "Tu restes quelqu'un. Chaque fois que je regarde.",
   "Du bleibst jemand. Jedes Mal, wenn ich hinsehe.",
   "Wciąż jesteś kimś. Za każdym razem, gdy patrzę."),
  "mm", ("Yes. Like that. Exactly like that.",
   "Sim. Assim. Exatamente assim.",
   "Oui. Comme ça. Exactement comme ça.",
   "Ja. So. Genau so.",
   "Tak. Właśnie tak. Dokładnie tak."),
  ("Do not stop.",
   "Não pare.",
   "N'arrête pas.",
   "Hör nicht auf.",
   "Nie przestawaj.")),

 # RUNNING OUT: she is stalling, and will not say why
 (("I am getting slower at these. You may have noticed.",
   "Estou ficando mais lenta nisso. Talvez tenha notado.",
   "Je deviens plus lente pour ça. Tu l'as peut-être vu.",
   "Ich werde langsamer dabei. Vielleicht ist es dir aufgefallen.",
   "Coraz wolniej mi to idzie. Może to widać."),
  ("Take as long as you like with the next one. Really.",
   "Demore o quanto quiser com o próximo. É sério.",
   "Prends tout le temps que tu veux pour le prochain. Vraiment.",
   "Lass dir mit dem nächsten so viel Zeit, wie du willst. Wirklich.",
   "Z następną nie spiesz się, ile chcesz. Naprawdę."),
  "why", ("No reason.",
   "Por nada.",
   "Pour rien.",
   "Kein Grund.",
   "Bez powodu."),
  "mm", ("Good.",
   "Bom.",
   "Bien.",
   "Gut.",
   "Dobrze."),
  ("There is no rush at all.",
   "Não há pressa nenhuma.",
   "Rien ne presse.",
   "Es gibt überhaupt keine Eile.",
   "Nie ma żadnego pośpiechu.")),

 (("I never asked if you minded. Friend. The word.",
   "Nunca perguntei se você se importava. Amigo. A palavra.",
   "Je n'ai jamais demandé si ça te gênait. L'ami. Le mot.",
   "Ich habe nie gefragt, ob es dich stört. Mein Freund. Das Wort.",
   "Nigdy nie pytałam, czy ci to nie przeszkadza. Przyjacielu. To słowo."),
  ("Before you do the next one, press something. Anything.",
   "Antes do próximo, aperte alguma coisa. Qualquer uma.",
   "Avant le prochain, appuie sur quelque chose. N'importe quoi.",
   "Bevor du das nächste machst, drück etwas. Irgendetwas.",
   "Przed następną naciśnij coś. Cokolwiek."),
  "mm", ("There you are.",
   "Aí está você.",
   "Te voilà.",
   "Da bist du.",
   "Jesteś."),
  "dots", ("That will do. That will do very well.",
   "Isso serve. Serve muito bem.",
   "Ça ira. Ça ira très bien.",
   "Das reicht. Das reicht sehr gut.",
   "Wystarczy. Bardzo dobrze wystarczy."),
  ("All right. Go on.",
   "Tudo bem. Vá em frente.",
   "D'accord. Vas-y.",
   "Na gut. Mach weiter.",
   "Dobrze. Śmiało.")),

 # she catches herself holding on, from the other side this time. the last board is where she says so.
 (("I made the next one easier, so you would finish it. Then harder, so you would not.",
   "Fiz o próximo mais fácil, para você terminar. Depois mais difícil, para não terminar.",
   "J'ai rendu le prochain plus facile, pour que tu le finisses. Puis plus dur, pour que non.",
   "Ich habe das nächste leichter gemacht, damit du es schaffst. Dann schwerer, damit nicht.",
   "Następną zrobiłam łatwiejszą, żeby szybko zniknęła. Potem trudniejszą, żeby nie."),
  ("I do not know which of us that was for.",
   "Não sei para qual de nós era.",
   "Je ne sais pas pour qui de nous deux c'était.",
   "Ich weiß nicht, für wen von uns beiden das war.",
   "Nie wiem, dla kogo z nas to było."),
  "why", ("Because I have started waiting for you. Between the boards. I never waited for anyone.",
   "Porque comecei a esperar por você. Entre os tabuleiros. Eu nunca esperei por ninguém.",
   "Parce que je me suis mise à t'attendre. Entre les plateaux. Je n'avais jamais attendu personne.",
   "Weil ich angefangen habe, auf dich zu warten. Zwischen den Brettern. Ich habe nie auf jemanden gewartet.",
   "Bo zaczęłam na ciebie czekać. Między planszami. Nigdy na nikogo nie czekałam."),
  "dots", ("Yes. I heard it too.",
   "Sim. Eu também ouvi.",
   "Oui. Je l'ai entendu aussi.",
   "Ja. Ich habe es auch gehört.",
   "Tak. Też to słyszałam."),
  ("There are a few more. I will stop there. I think.",
   "Faltam alguns. Vou parar neles. Acho.",
   "Il en reste quelques-uns. Je m'arrêterai là. Je crois.",
   "Es sind noch ein paar. Dann höre ich auf. Glaube ich.",
   "Zostało jeszcze kilka. Na nich skończę. Chyba."))
]


def talk_key(n, part):
    return f"angler.talk.c{n:02d}.{part}" + REWRITTEN.get((n, part), "")


def rows():
    yield "angler.puzzle.hint2", HINT2, HINT_NOTE
    for name, words in REPLIES.items():
        yield f"angler.reply.{name}", words, REPLY_NOTE
    for name, words in REMARKS.items():
        yield f"angler.remark.{name}", words, NOTE
    for n, (a, b, _r1, one, _r2, two, end) in enumerate(TALKS, 1):
        for part, words in (("a", a), ("b", b), ("r1", one), ("r2", two), ("end", end)):
            yield talk_key(n, part), words, NOTE
    for name, words in LINES.items():
        yield f"angler.puzzle.{name}", words, NOTE


def source():
    say = []
    for n, (a, b, r1, one, r2, two, end) in enumerate(TALKS, 1):
        for part, name, words in ((1, "a", a), (2, "b", b), (3, "r1", one), (4, "r2", two), (5, "end", end)):
            key = talk_key(n, name)
            say.append(f"        case {n * 10 + part}")
            if "{$score}" in words[0]:
                say.append(f'            anglerSay(loc$("{key}", "score", PUZZLE_BEST_SCORE))')
            else:
                say.append(f'            puzzleSay(lockey$("{key}"))')
            say.append("        endcase")
    kinds = {name: i for i, name in enumerate(REPLIES)}
    replies = []
    for n, talk in enumerate(TALKS, 1):
        replies.append(f"    PUZZLE_REPLY_OF({(n - 1) * 2}) = {kinds[talk[2]]}")
        replies.append(f"    PUZZLE_REPLY_OF({(n - 1) * 2 + 1}) = {kinds[talk[4]]}")
    set_reply = []
    for name, i in kinds.items():
        set_reply += [f"        case {i}", f'            set text loc srText, "angler.reply.{name}"', "        endcase"]
    return "\n".join([
        "` MADE BY build/make_ava_talk.py, which is where the words are. do not change this by hand.",
        "goto puzzle_talk_exit",
        "",
        "setup_puzzle_talk:",
        *replies,
        "return",
        "",
        "puzzle_talk_exit:",
        "",
        "",
        "` she says one part of a talk. the parts are 1 and 2, then 3 or 4 by the reply, and 5.",
        "function puzzleTalkSay(tsTalk, tsPart)",
        "    select tsTalk * 10 + tsPart",
        *say,
        "    endselect",
        "endfunction",
        "",
        "",
        "` the words of a reply on a text",
        "function puzzleReplyWords(srText, srKind)",
        "    select srKind",
        *set_reply,
        "    endselect",
        "endfunction",
        "",
    ])


def main():
    assert len(TALKS) == 19
    for key, words, _ in rows():
        for w in words:
            low = w.lower()
            assert " free" not in low and "love" not in low, key
        assert ("{$score}" in words[0]) == all("{$score}" in w for w in words), key
    OUT.write_text(source(), encoding="utf-8", newline="\n")

    raw = TABLE.read_bytes().decode("utf-8")
    have = {r[0] for r in csv.reader(io.StringIO(raw)) if r}
    out = io.StringIO()
    w = csv.writer(out, lineterminator="\n")
    added = 0
    for key, (en, pt, fr, de, pl), note in rows():
        if key not in have:
            w.writerow([key, en, pt, note, "", "", "FALSE", "", fr, de, pl, "FALSE", "FALSE", "FALSE", "", "", ""])
            added += 1
    if not raw.endswith("\n"):
        raw += "\n"
    TABLE.write_bytes((raw + out.getvalue()).encode("utf-8"))
    print(f"wrote {OUT.name}, added {added} rows")


if __name__ == "__main__":
    main()
