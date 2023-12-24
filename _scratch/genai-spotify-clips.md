## Using Generative AI to produce Spotify Clips

- Some Spotify songs and artist pages include [Clips](https://clips.byspotify.com/). Clips are 
[under-30-second vertical videos](https://artists.spotify.com/blog/spotify-clips-get-started-short-form-video-stream-on) uploaded directly to Spotify for Artists. They’re designed to put artists and their music front-and-center, so they can be attached to their artist profile, to a song, to an album, or to an upcoming release. 
- Google recently announced [VideoPoet](https://sites.research.google/videopoet/), a large language model for zero-shot video generation. 
- Using a model like VideoPoet, you could generate visual stories. They did that in this [video](https://www.youtube.com/shorts/70wZKfx6Ylk). As they explain: 

    > They have produced a short movie composed of many short clips generated by the model. For the script, we asked Bard to write a series of prompts to detail a short story about a traveling raccoon. We then generated video clips for each prompt, and stitched together all resulting clips

    One thing to note here is that they produced a YouTube Short, which is also a vertical video. This aligns nicely with the Spotify Clips format.

- Typically, songs and albums tell a story. Therefore, instead of using Generative AI to generate the story, you can use the lyrics directly. You might use GenAI to make sure that the story is less abstract and more linear which would help make generated video clips more cohesive. Using the same technique as Google, you could then write a series of prompts which are then stitched together into a single clip. 
- In many cases, Spotify lyrics are synced to timestamps so this is yet additional metadata Spotify could use to align the generated clips with the song lyrics. 
- Generate visual story script

    ```text
    Using the song information below as context, generate a script for a story that visually represents the song lyrics. Ensure that the script is broken down into individual scenes and the scenes are linear and cohesively transition.

    <song>
    <title>A Hundred Dead and Loving Souls</title>
    <lyrics>
    I am the sand stuck under miles of rock
    watching the dry air sweep away my thoughts
    as we travel protected by shards of glass
    driving through the history of a million memories

    In a sea of a hundred dead and loving souls
    In a sea of a hundred dead and loving souls
    In a sea of a hundred dead and loving souls
    Climbing a thousand unclimbed trees
    I don't know I want to know how it would feel
    to be on top of one of these!

    I am the root
    quickly ascending
    up through the surrounding dirt
    quickly devoured by a hungry sun
    quickly devoured by a hungry son
    who is my father
    and we are alike
    we look alike we talk alike
    we are alike
    we are alike
    we are alike we look alike
    we talk alike

    Thats why Im hungry
    like the bellowing sky
    this is no lie
    as sure as light
    entering eye
    want to stay high
    everything's cool too cool so cool
    and I remember that everything falls

    So I take a look around me now, as I speak
    and I imagine a disinterested crowd
    talking aloud
    and I think of what I want most
    and the crowd disappears
    and a fixated gaze envelopes my wondering mind
    and then she recruits and takes me to this place
    I've been there before and that's where I saw her face

    In a sea of a hundred dead and loving souls
    In a sea of a hundred dead and loving souls
    In a sea of a hundred dead and loving souls
    Climbing a thousand unclimbed trees
    I don't know I want to know how it would feel
    to be on top of one of these!
    </lyrics>
    </song>

    Story: 
    ```

    ```markdown
    # Script for A Hundred Dead and Loving Souls

    ## Scene 1: The Sand
    - A close-up shot of a grain of sand under a microscope, showing its intricate structure and texture.
    - The camera zooms out to reveal that the sand is part of a vast desert landscape, with dunes and rocks stretching to the horizon.
    - A voice-over of the protagonist (a young man) narrates the first verse of the song, expressing his feelings of being trapped and forgotten under the weight of the world.
    - The camera pans to show a car driving on a dusty road, with the protagonist and his father inside. They are both wearing sunglasses and hats to protect themselves from the sun.
    - The car is filled with boxes and bags, indicating that they are moving to a new place.
    - The father is driving and listening to the radio, while the protagonist is looking out the window, bored and restless.
    - The radio plays a news report about a nuclear fusion experiment in South Korea, followed by a song by a popular band.
    - The father comments on the news, saying that it is a breakthrough for humanity and that he hopes to see it in his lifetime.
    - The protagonist does not respond, but thinks to himself that he does not care about anything anymore.

    ## Scene 2: The Sea
    - A montage of shots showing the protagonist and his father arriving at their new home, a small town by the sea.
    - The father is excited and optimistic, while the protagonist is indifferent and detached.
    - The father unpacks their belongings and sets up their new house, while the protagonist wanders around the town, exploring the beach, the pier, and the streets.
    - The protagonist sees a group of teenagers hanging out at a skate park, laughing and having fun. He feels out of place and lonely.
    - The protagonist walks to the edge of the pier, where he sees a sign that says "No diving".
    - He looks down at the water, where he sees hundreds of dead fish floating on the surface, victims of a recent oil spill.
    - He hears the chorus of the song in his head, imagining himself diving into the sea of a hundred dead and loving souls, and climbing a thousand unclimbed trees.
    - He wonders what it would feel like to be free and alive, instead of being stuck and dead inside.

    ## Scene 3: The Root
    - A flashback scene, showing the protagonist as a child, playing in a garden with his father.
    - The father is a botanist, and he teaches his son about different plants and flowers, showing him how to care for them and appreciate their beauty.
    - The son is curious and eager, and he asks his father many questions about the natural world.
    - The father tells his son that he is a root, quickly ascending up through the surrounding dirt, and that he will grow into a strong and beautiful tree one day.
    - The son smiles and hugs his father, saying that he loves him and that he wants to be like him.
    - The father smiles and hugs his son back, saying that he loves him too and that he is proud of him.

    ## Scene 4: The Sun
    - A flash-forward scene, showing the protagonist as a young adult, working in a laboratory with his father.
    - The father is a nuclear physicist, and he is working on a project to harness the power of nuclear fusion, the same process that fuels the sun.
    - The son is his assistant, and he helps his father with the experiments and calculations, following his instructions and commands.
    - The father tells his son that he is a hungry sun, quickly devoured by a hungry son, and that he will make a great contribution to science and humanity one day.
    - The son nods and agrees, but he does not look happy or satisfied. He feels pressured and obligated to follow his father's footsteps, even though he does not share his passion or vision.
    - The son thinks to himself that he is hungry, like the bellowing sky, and that he wants to stay high, away from the reality and expectations that weigh him down.

    ## Scene 5: The Crowd
    - A present-day scene, showing the protagonist at a party, surrounded by a crowd of people.
    - The party is hosted by one of the teenagers that he saw at the skate park, who invited him out of pity and curiosity.
    - The party is loud and chaotic, with music, drinks, and drugs everywhere.
    - The protagonist feels uncomfortable and anxious, and he does not fit in with the crowd. He does not know anyone, and no one knows him.
    - He tries to make small talk with some of the guests, but they are either rude, bored, or high, and they do not pay attention to him.
    - He feels invisible and insignificant, and he wishes he could disappear.
    - He takes a look around him, as he speaks, and he imagines a disinterested crowd, talking aloud, and he thinks of what he wants most.

    ## Scene 6: The Gaze
    - A close-up shot of a pair of eyes, staring intently at the protagonist.
    - The eyes belong to a girl, who is also at the party, but who is different from the rest of the crowd. She is quiet and mysterious, and she has a captivating and mysterious aura.
    - The girl notices the protagonist, and she is intrigued by him. She sees something in him that no one else sees, something that she relates to and understands.
    - She walks up to him, and she introduces herself. She says her name is Luna, and that she likes his song. She says she heard him humming it to himself, and that she knows the lyrics by heart.
    - The protagonist is surprised and confused, and he asks her how she knows his song. He says he wrote it himself, and that he never shared it with anyone.
    - The girl smiles and says that she knows his song, because she wrote it too. She says she has a gift, or a curse, depending on how you look at it. She says she can hear the songs that people sing in their souls, and that she can sing them back to them.
    - The protagonist is shocked and amazed, and he asks her to prove it. He asks her to sing his song, or any song that he knows.
    - The girl nods and agrees, and she sings his song, word for word, note for note, perfectly. She sings it with emotion and expression, as if she wrote it herself.
    - The protagonist is speechless and mesmerized, and he feels a connection with the girl that he never felt with anyone before. He feels like she knows him, and he wants to know her.
    - The girl stops singing, and she looks at him with a fixated gaze, that envelopes his wondering mind. She says she likes him, and she asks him if he wants to go with her to a place that she knows.
    - The protagonist nods and agrees, and he follows her out of the party, leaving the crowd behind.

    ## Scene 7: The Place
    - A montage of shots showing the protagonist and the girl driving to a secluded spot in the woods, where they park their car and get out.
    - The spot is a clearing, where there is a large tree, with a rope swing hanging from one of its branches.
    - The tree is the only one of its kind in the area, and it stands out from the rest of the forest. It is tall and majestic, with green leaves and red flowers.
    - The girl tells the protagonist that this is her place, and that she comes here often to escape from the world. She says she loves this tree, and that she calls it Luna, after herself.
    - The protagonist tells the girl that this is his place too, and that he has been here before. He says he saw this tree in his dreams, and that he always wanted to climb it.
    - The girl smiles and says that they have more in common than they thought, and that they are meant to be together. She says she wants to show him something, and she asks him to follow her.
    - The girl climbs the tree, using the rope swing as a support. She is agile and graceful, and she reaches the top in no time.
    - The protagonist follows her, using the rope swing as well. He is clumsy and nervous, and he struggles to keep up with her.
    - The girl reaches the top of the tree, where she finds a small platform, made of wood and rope. She sits on the platform, and she looks at the view.
    - The view is breathtaking, and it shows the sea, the town, and the sky, all in one panorama. The sun is setting, and it casts a golden glow on everything.
    - The girl waves at the protagonist, and she invites him to join her. She says this is the best place to watch the sunset, and that she wants to share it with him.
    - The protagonist reaches the top of the tree, where he joins the girl on the platform. He sits next to her, and he looks at the view. He is awestruck, and he agrees that this is the best place to watch the sunset.
    - The girl leans in and kisses the protagonist, and he kisses her back. They embrace each other, and they watch the sunset together.
    - The chorus of the song plays in the background, as the scene fades to black.
    ```

- Generate prompts (note that in this case, I'll using DALL-E). However, you could imagine creating a prompt for a video clip for each of the bullet points in each scene.

    ```text
    Using storyboarding techniques, for each scene, generate a single prompt to use as input for DALL-E to generate an image. Since there are 7 scenes, there should be only 7 prompts. Distill as much of the concepts from each of the bullet points in each scene.
    ```

    ```text
    Scene 1: The Sand
        A grain of sand under a microscope and a car driving on a dusty road in a desert
    Scene 2: The Sea
        A boy standing on a pier looking at a sea of dead fish and a sign that says “No diving”
    Scene 3: The Root
        A father and a son playing in a garden with different plants and flowers
    Scene 4: The Sun
        A father and a son working in a laboratory with a nuclear fusion reactor
    Scene 5: The Crowd
        A boy at a party surrounded by a crowd of people with music, drinks, and drugs
    Scene 6: The Gaze
        A girl with a pair of eyes that can hear the songs that people sing in their souls
    Scene 7: The Place
        A boy and a girl sitting on a platform on top of a tree with a rope swing and a view of the sea, the town, and the sunset
    ```

- Generate images for each of the scenes

    ```text
    Use the prompts for each of the scenes to generate images with DALL-E. Render them as pencil art.
    ```

    TODO: Include images

- Improvements
  - Prompts. My prompts were naive and not descriptive enough. The model did a good enough job interpreting what I wanted. But this could be better.
  - Code. This was a thought experiment and proof of concept. I could make this reproducible and more sophisticated by building code-first solutions.
  - Videos. In this case, I stopped at images because I knew I could do this using Copilot. I didn't actually search to see whether VideoPoet is available for use or if similar open-source models exist. However, the next natural step would be to actually generate the video clips.  